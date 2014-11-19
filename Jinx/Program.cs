using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Collision = LeagueSharp.Common.Collision;
namespace Jinx
{
    class Program
    {
        public const string ChampionName = "Jinx";

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;

        //Spells
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        
        public static int WMANA;
        public static int EMANA;
        public static int RMANA;
        //Menu
        public static Menu Config;

        private static Obj_AI_Hero Player;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;
            if (Player.BaseSkinName != ChampionName) return;

            //Create the spells
            Q = new Spell(SpellSlot.Q, float.MaxValue);
            W = new Spell(SpellSlot.W, 1500f);
            E = new Spell(SpellSlot.E, 900f);
            R = new Spell(SpellSlot.R, 25000f);

            W.SetSkillshot(0.6f, 60f, 3300f, true, SkillshotType.SkillshotLine);
            E.SetSkillshot(1.1f, 20f, 1750f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.6f, 150f, 2000f, false, SkillshotType.SkillshotLine);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            //Create the menu
            Config = new Menu(ChampionName, ChampionName, true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            //Orbwalker submenu
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));

            //Load the orbwalker and add it to the submenu.
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.AddToMainMenu();

            //Add the events we are going to use:
            Game.OnGameUpdate += Game_OnGameUpdate;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            ManaMenager();

            if (ObjectManager.Player.Mana > RMANA + EMANA && E.IsReady())
            {

                var t = SimpleTs.GetTarget(900f, SimpleTs.DamageType.Physical);

                var autoEi = true;
                var autoEs = true;
                var autoEd = true;
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(E.Range)))
                {
                    E.CastIfHitchanceEquals(enemy, HitChance.Immobile);
                    if (autoEs && enemy.HasBuffOfType(BuffType.Slow) &&  enemy.Path.Count() > 0 &&
                        enemy.Path[0].Distance(Player.ServerPosition) > Player.Distance(enemy))
                    {
                        var castPosition =
                            Prediction.GetPrediction(
                                new PredictionInput
                                {
                                    Unit = enemy,
                                    Delay = 1f,
                                    Radius = 50f,
                                    Speed = 1750f,
                                    Range = 900f,
                                    Type = SkillshotType.SkillshotCircle,
                                }).CastPosition;

                        if (GetSlowEndTime(enemy) >= (Game.Time + E.Delay + 0.5f))
                            E.Cast(castPosition);
                    }

                    if (autoEi &&
                        (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                         enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                         enemy.HasBuffOfType(BuffType.Taunt)|| enemy.IsStunned))

                        E.CastIfHitchanceEquals(enemy, HitChance.High);
                   
                    
                    if (autoEd && enemy.IsDashing())
                        E.CastIfHitchanceEquals(enemy, HitChance.Dashing);
                }
            }

            if (Q.IsReady())
            {
                var t = SimpleTs.GetTarget(bonusRange(), SimpleTs.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    var distance = GetRealDistance(t);
                    var powPowRange = GetRealPowPowRange(t);
                    var adDamage = ObjectManager.Player.GetAutoAttackDamage(t);

                    if (!FishBoneActive && (distance > powPowRange) && (ObjectManager.Player.Mana > RMANA + WMANA || adDamage > t.Health))
                    {
                        if (Orbwalker.ActiveMode.ToString() == "Combo")
                            Q.Cast();
                        else if ((Orbwalker.ActiveMode.ToString() == "Mixed" || Orbwalker.ActiveMode.ToString() == "LaneClear") && ObjectManager.Player.Mana > RMANA + WMANA + EMANA && distance < bonusRange() + 120 && t.Path.Count() > 0 &&
                        t.Path[0].Distance(Player.ServerPosition) < Player.Distance(t))
                            Q.Cast();
                        else if ((Orbwalker.ActiveMode.ToString() == "Mixed" || Orbwalker.ActiveMode.ToString() == "LaneClear") && ObjectManager.Player.Mana > RMANA + WMANA + EMANA && distance < bonusRange())
                            Q.Cast();
                    }
                    else if (Orbwalker.ActiveMode.ToString() == "Combo" && FishBoneActive && (distance < powPowRange))
                        Q.Cast();
                    else if ((Orbwalker.ActiveMode.ToString() == "Mixed" || Orbwalker.ActiveMode.ToString() == "LaneClear") && FishBoneActive && (distance > bonusRange() || distance < powPowRange))
                         Q.Cast();
                }
                else if (FishBoneActive && (Orbwalker.ActiveMode.ToString() == "Mixed" || Orbwalker.ActiveMode.ToString() == "LaneClear"))
                    Q.Cast();
                else if (!FishBoneActive && (Orbwalker.ActiveMode.ToString() == "Combo"))
                    Q.Cast();
            }

            if (W.IsReady())
            {
                var t = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    var wDmg = W.GetDamage(t);
                    if (GetRealDistance(t) > GetRealPowPowRange(t) && wDmg > t.Health)
                        W.Cast(t, true);
                    else if (Orbwalker.ActiveMode.ToString() == "Combo"  && ObjectManager.Player.Mana > RMANA + WMANA && CountEnemies(ObjectManager.Player, GetRealPowPowRange(t)) == 0 && !ObjectManager.Player.IsAutoAttacking)
                        W.CastIfHitchanceEquals(t, HitChance.High, true);
                    else if (((Orbwalker.ActiveMode.ToString() == "Mixed" || Orbwalker.ActiveMode.ToString() == "LaneClear") && ObjectManager.Player.Mana > RMANA + EMANA + WMANA + WMANA) && CountEnemies(ObjectManager.Player, GetRealPowPowRange(t)) == 0)
                            W.CastIfHitchanceEquals(t, HitChance.High, true);
                    else if (ObjectManager.Player.Mana > RMANA + WMANA && CountEnemies(ObjectManager.Player, GetRealPowPowRange(t)) == 0)
                    {
                        foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(E.Range - 150)))
                        {
                            if (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                             enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                             enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Slow))
                                W.CastIfHitchanceEquals(t, HitChance.High, true);
                        }
                    }
                }
            }

            if (R.IsReady() && !ObjectManager.Player.IsAutoAttacking)
            {
                var maxR = 2500f;
                var t = SimpleTs.GetTarget(maxR, SimpleTs.DamageType.Physical);

                if (t.IsValidTarget())
                {
                    var castPosition =
                            Prediction.GetPrediction(
                                new PredictionInput
                                {
                                    Unit = t,
                                    Delay = 0.6f,
                                    Radius = 150f,
                                    Speed = 2000f,
                                    Range = maxR,
                                    Type = SkillshotType.SkillshotCircle,
                                }).CastPosition;
                    var distance = GetRealDistance(t);
                    var rDamage = R.GetDamage(t);
                    var powPowRange = GetRealPowPowRange(t);
                    if (rDamage > t.Health && CountAlliesNearTarget(t, 600) == 0 && CountEnemies(ObjectManager.Player, 200f) == 0 && distance > bonusRange() + 200 && IsCollidingWithChamps(castPosition,300))
                        R.CastIfHitchanceEquals(t, HitChance.High, true);
                    else if (ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.4 && rDamage * 1.4 > t.Health && CountEnemies(ObjectManager.Player, GetRealPowPowRange(t)) > 0 && distance > 300)
                        R.CastIfHitchanceEquals(t, HitChance.High, true);
                    else if (rDamage  > t.Health && CountEnemies(t, 200) > 2 )
                        R.CastIfHitchanceEquals(t, HitChance.High, true);
                }
            }
        }
        public static bool IsCollidingWithChamps( Vector3 targetpos, float width)
        {
            var input = new PredictionInput
            {
                Radius = width,
                Unit = ObjectManager.Player,
            };

            input.CollisionObjects[0] = CollisionableObjects.Heroes;
            return Collision.GetCollision(new List<Vector3> { targetpos }, input).Any(); //x => x.NetworkId != targetnetid, hard to realize with teamult
        }
        public static float bonusRange()
        {
            return 620f + ObjectManager.Player.BoundingRadius + 50 + 25 * ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level; 
        }

        private static bool FishBoneActive
        {
            get { return Math.Abs(ObjectManager.Player.AttackRange - 525f) > float.Epsilon; }
        }

        private static int PowPowStacks
        {
            get
            {
                return
                    ObjectManager.Player.Buffs.Where(buff => buff.DisplayName.ToLower() == "jinxqramp")
                        .Select(buff => buff.Count)
                        .FirstOrDefault();
            }
        }
        private static int CountEnemies(Obj_AI_Base target, float range)
        {
            return
                ObjectManager.Get<Obj_AI_Hero>()
                    .Count(
                        hero =>
                            hero.IsValidTarget() && hero.Team != ObjectManager.Player.Team &&
                            hero.ServerPosition.Distance(target.ServerPosition) <= range);
        }
        private static int CountAlliesNearTarget(Obj_AI_Base target, float range)
        {
            return
                ObjectManager.Get<Obj_AI_Hero>()
                    .Count(
                        hero =>
                            hero.Team == ObjectManager.Player.Team &&
                            hero.ServerPosition.Distance(target.ServerPosition) <= range);
        }

        private static float GetRealPowPowRange(GameObject target)
        {
            return 600f + ObjectManager.Player.BoundingRadius + target.BoundingRadius;
        }

        private static float GetRealDistance(GameObject target)
        {
            return ObjectManager.Player.Position.Distance(target.Position) + ObjectManager.Player.BoundingRadius +
                   target.BoundingRadius;
        }

        private static float GetSlowEndTime(Obj_AI_Base target)
        {
            return
                target.Buffs.OrderByDescending(buff => buff.EndTime - Game.Time)
                    .Where(buff => buff.Type == BuffType.Slow)
                    .Select(buff => buff.EndTime)
                    .FirstOrDefault();
        }

        public static void ManaMenager()
        {
            WMANA = 40 + 10 * W.Level;
            EMANA = 50;
            if (!R.IsReady())
                RMANA = WMANA - 10;
            else
                RMANA = 100;
        }
    }

}
