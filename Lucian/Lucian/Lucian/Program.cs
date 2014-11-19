using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Collision = LeagueSharp.Common.Collision;
namespace Lucian
{
    class Program
    {
        public const string ChampionName = "Lucian";

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;

        //Spells
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell Q2;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        public static int WMANA;
        public static int EMANA;
        public static int RMANA;

        public static bool DoubleHit = false;
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
            Q = new Spell(SpellSlot.Q, 675);
            Q2 = new Spell(SpellSlot.Q, 1100);
            W = new Spell(SpellSlot.W, 1000);
            E = new Spell(SpellSlot.E, 900f);
            R = new Spell(SpellSlot.R, 25000f);

            Q2.SetSkillshot(0.25f, 65f, 1100f, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.30f, 80f, 1600f, true, SkillshotType.SkillshotLine);

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
                    if (autoEs && enemy.HasBuffOfType(BuffType.Slow) && enemy.Path.Count() > 0 &&
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
                         enemy.HasBuffOfType(BuffType.Taunt) || enemy.IsStunned))

                        E.CastIfHitchanceEquals(enemy, HitChance.High);


                    if (autoEd && enemy.IsDashing())
                        E.CastIfHitchanceEquals(enemy, HitChance.Dashing);
                }
            }

            if (Q.IsReady() && !DoubleHit)
            {
                var t = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    var qDmg = Q.GetDamage(t);
                    if (qDmg > t.Health)
                        Q.Cast(t, true);
                     else if (Orbwalker.ActiveMode.ToString() == "Combo" && ObjectManager.Player.Mana > RMANA + WMANA)
                         Q.Cast(t, true);
                     else if (((Orbwalker.ActiveMode.ToString() == "Mixed" || Orbwalker.ActiveMode.ToString() == "LaneClear") && ObjectManager.Player.Mana > RMANA + EMANA + WMANA + WMANA))
                         Q.Cast(t, true);
                }
                var t2 = SimpleTs.GetTarget(Q2.Range, SimpleTs.DamageType.Physical);
                if (t.IsValidTarget() && QMinion.IsValidTarget())
                {
                    var qDmg = Q.GetDamage(t);
                    if (qDmg > t.Health)
                        Q.CastOnUnit(QMinion,true);
                     else if (Orbwalker.ActiveMode.ToString() == "Combo" && ObjectManager.Player.Mana > RMANA + WMANA)
                         Q.CastOnUnit(QMinion,true);
                     else if (((Orbwalker.ActiveMode.ToString() == "Mixed" || Orbwalker.ActiveMode.ToString() == "LaneClear") && ObjectManager.Player.Mana > RMANA + EMANA + WMANA + WMANA))
                         Q.CastOnUnit(QMinion,true);
                }
            }

            if (W.IsReady())
            {
                var t = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    var wDmg = W.GetDamage(t);
                    if (GetRealDistance(t) > GetRealPowPowRange(t) && wDmg > t.Health)
                        W.Cast(t, true);
                    else if (Orbwalker.ActiveMode.ToString() == "Combo" && ObjectManager.Player.Mana > RMANA + WMANA && CountEnemies(ObjectManager.Player, GetRealPowPowRange(t)) == 0 && !ObjectManager.Player.IsAutoAttacking)
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
                    if (rDamage > t.Health && CountAlliesNearTarget(t, 600) == 0 && CountEnemies(ObjectManager.Player, 200f) == 0 && distance > bonusRange() + 200 && IsCollidingWithChamps(castPosition, 300))
                        R.CastIfHitchanceEquals(t, HitChance.High, true);
                    else if (ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.4 && rDamage * 1.4 > t.Health && CountEnemies(ObjectManager.Player, GetRealPowPowRange(t)) > 0 && distance > 300)
                        R.CastIfHitchanceEquals(t, HitChance.High, true);
                    else if (rDamage > t.Health && CountEnemies(t, 300) > 2)
                        R.CastIfHitchanceEquals(t, HitChance.High, true);
                }
            }
        }

        public static Obj_AI_Base QMinion
        {
            get
            {
                var vTarget = SimpleTs.GetTarget(Q2.Range, SimpleTs.DamageType.Physical);
                var vMinions = MinionManager.GetMinions(
                    ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly,
                    MinionOrderTypes.None);

                return (from vMinion in vMinions.Where(vMinion => vMinion.IsValidTarget(Q.Range))
                    let endPoint =
                        vMinion.ServerPosition.To2D()
                            .Extend(ObjectManager.Player.ServerPosition.To2D(), -Q2.Range)
                            .To3D()
                    where
                        Intersection(
                            ObjectManager.Player.ServerPosition.To2D(), endPoint.To2D(), vTarget.ServerPosition.To2D(),
                            vTarget.BoundingRadius + Q.Width / 2)
                    select vMinion).FirstOrDefault();
            }
        }

        public static bool Intersection(Vector2 p1, Vector2 p2, Vector2 pC, float radius)
        {
            var p3 = new Vector2(pC.X + radius, pC.Y + radius);
            var m = ((p2.Y - p1.Y) / (p2.X - p1.X));
            var constant = (m * p1.X) - p1.Y;
            var b = -(2f * ((m * constant) + p3.X + (m * p3.Y)));
            var a = (1 + (m * m));
            var c = ((p3.X * p3.X) + (p3.Y * p3.Y) - (radius * radius) + (2f * constant * p3.Y) + (constant * constant));
            var d = ((b * b) - (4f * a * c));
            return d > 0;
        }

        public static bool IsCollidingWithChamps(Vector3 targetpos, float width)
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
        public static void Game_OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs spell)
        {
            if (!unit.IsMe) return;
            if (spell.SData.Name.Contains("summoner")) return;

            if (spell.SData.Name.Contains("Attack"))
            {
                Utility.DelayAction.Add(50, () =>
                {
                    DoubleHit = false;
                    Utility.DelayAction.ActionList.Clear();
                });

            }
            else if (spell.SData.Name.Contains("Lucian") && !spell.SData.Name.Contains("Attack"))
            {
                Orbwalking.ResetAutoAttackTimer();

                Utility.DelayAction.Add(6000, () =>
                {
                    if (DoubleHit)
                        DoubleHit = false;
                });

                DoubleHit = true;
            }
        }
    }

}
