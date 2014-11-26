using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;

using Color = System.Drawing.Color;

namespace Kalista
{
    internal class Program
    {
        internal const string CHAMP_NAME = "Kalista";
        internal static Obj_AI_Hero player = ObjectManager.Player;

        internal static Spell Q, W, E, R;
        internal static readonly List<Spell> spellList = new List<Spell>();

        internal static Menu menu;
        internal static Orbwalking.Orbwalker OW;

        public static int QMANA;
        public static int WMANA;
        public static int EMANA;
        public static int RMANA;

        internal static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        internal static void Game_OnGameLoad(EventArgs args)
        {
            // Validate Champion
            if (player.ChampionName != CHAMP_NAME)
                return;

            // Initialize spells
            Q = new Spell(SpellSlot.Q, 1200);
            W = new Spell(SpellSlot.W, 5000);
            E = new Spell(SpellSlot.E, 1000);
            R = new Spell(SpellSlot.R, 1500);

            // Add to spell list
            spellList.AddRange(new[] { Q, W, E, R });

            // Finetune spells
            Q.SetSkillshot(0.25f, 40, 1200, true, SkillshotType.SkillshotLine);

            // Setup menu
            SetuptMenu();

            // Register additional events
            Game.OnGameUpdate += Game_OnGameUpdate;
            Obj_AI_Hero.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
        }

        internal static void Game_OnGameUpdate(EventArgs args)
        {
            ManaMenager();
            if (Q.IsReady())
            {
                var t = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    var qDmg = Q.GetDamage(t);

                    if (qDmg > t.Health)
                        Q.Cast(t, true);
                    else if (OW.ActiveMode.ToString() == "Combo" && ObjectManager.Player.Mana > EMANA + QMANA && GetRealDistance(t) > Orbwalking.GetRealAutoAttackRange(player))
                        Q.CastIfHitchanceEquals(t, HitChance.High, true);
                    else if (((OW.ActiveMode.ToString() == "Mixed" || OW.ActiveMode.ToString() == "LaneClear") && ObjectManager.Player.Mana > RMANA + EMANA + WMANA + QMANA) )
                        Q.CastIfHitchanceEquals(t, HitChance.High, true);
                    else if (ObjectManager.Player.Mana > EMANA + QMANA)
                    {
                        foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(E.Range - 150)))
                        {
                            if (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                             enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                             enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Slow))
                                Q.CastIfHitchanceEquals(t, HitChance.High, true);
                        }
                    }
                }
            }

            // Check killsteal
            if (E.IsReady())
            {
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsValidTarget(E.Range)))
                {

                    if (GetRendDamage(enemy) > enemy.Health)
                    {
                        E.Cast();
                        break;
                    }
                }
            }
            if (E.IsReady() && ObjectManager.Player.Mana > EMANA + 30)
            {
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsValidTarget(E.Range)))
                {
                    var buff = enemy.Buffs.FirstOrDefault(b => b.DisplayName.ToLower() == "kalistaexpungemarker");

                    if (buff != null && buff.EndTime - Game.Time < 0.4f)
                        E.Cast();
                }
            }
        }

        private static float GetRealDistance(GameObject target)
        {
            return ObjectManager.Player.Position.Distance(target.Position) + ObjectManager.Player.BoundingRadius +
                   target.BoundingRadius;
        }

        public static double GetRendDamage(Obj_AI_Base target)
        {
            var buff = target.Buffs.FirstOrDefault(b => b.DisplayName.ToLower() == "kalistaexpungemarker");
            if (buff != null)
            {
                double damage = (10 + 10 * ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Level) + 0.6 * ObjectManager.Player.FlatPhysicalDamageMod;
                damage += buff.Count * (new double[] { 0, 5, 9, 14, 20, 27 }[ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Level] + (0.12 + 0.03 * ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Level) * ObjectManager.Player.FlatPhysicalDamageMod);
                double dmg = ObjectManager.Player.CalcDamage(target, Damage.DamageType.Physical, damage);
                return dmg;
            }
            return 0;
        }
        

        internal static void OnWaveClear()
        {
            // Mana check
            if ((player.Mana / player.MaxMana) * 100 < menu.SubMenu("waveClear").Item("waveMana").GetValue<Slider>().Value)
                return;

            bool useE = menu.SubMenu("waveClear").Item("waveUseE").GetValue<bool>();

            if (useE && E.IsReady())
            {
                int hitNumber = menu.SubMenu("waveClear").Item("waveNumE").GetValue<Slider>().Value;

                // Get surrounding
                var minions = MinionManager.GetMinions(player.Position, E.Range);

                // Check if enough minions die with E
                int conditionMet = 0;
                foreach (var minion in minions)
                {
                    if (player.GetSpellDamage(minion, SpellSlot.E) > minion.Health)
                        conditionMet++;
                }

                // Cast on condition met
                if (conditionMet >= hitNumber)
                    E.Cast();
            }
        }

        internal static void OnJungleClear()
        {
            bool useE = menu.SubMenu("jungleClear").Item("jungleUseE").GetValue<bool>();

            if (useE && E.IsReady())
            {
                var minions = MinionManager.GetMinions(player.Position, E.Range, MinionTypes.All, MinionTeam.Neutral);

                // Check if a jungle mob can die with E
                foreach (var minion in minions)
                {
                    if (player.GetSpellDamage(minion, SpellSlot.E) > minion.Health)
                    {
                        E.Cast();
                        break;
                    }
                }
            }
        }

        internal static void OnFlee()
        {
            //bool useWalljump = menu.SubMenu("flee").Item("fleeWalljump").GetValue<bool>();
            bool useAA = menu.SubMenu("flee").Item("fleeAA").GetValue<bool>();

            if (useAA)
            {
                var dashObject = GetDashObject();
                if (dashObject != null)
                    Orbwalking.Orbwalk(dashObject, Game.CursorPos);
                else
                    Orbwalking.Orbwalk(null, Game.CursorPos);
            }
        }

        internal static Obj_AI_Base GetDashObject()
        {
            float realAArange = Orbwalking.GetRealAutoAttackRange(player);

            var objects = ObjectManager.Get<Obj_AI_Base>().Where(o => o.IsValidTarget(realAArange));
            Vector2 apexPoint = player.ServerPosition.To2D() + (player.ServerPosition.To2D() - Game.CursorPos.To2D()).Normalized() * realAArange;

            Obj_AI_Base target = null;

            foreach (var obj in objects)
            {
                if (VectorHelper.IsLyingInCone(obj.ServerPosition.To2D(), apexPoint, player.ServerPosition.To2D(), realAArange))
                {
                    if (target == null || target.Distance(apexPoint, true) > obj.Distance(apexPoint, true))
                        target = obj;
                }
            }

            return target;
        }


        internal static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.Name == "KalistaExpungeWrapper")
                    Utility.DelayAction.Add(250, Orbwalking.ResetAutoAttackTimer);
            }
        }

        internal static void SetuptMenu()
        {
            // Create menu
            menu = new Menu("[Hellsing] " + CHAMP_NAME, "hells" + CHAMP_NAME, true);

            // Target selector
            Menu targetSelector = new Menu("Target Selector", "ts");
            SimpleTs.AddToMenu(targetSelector);
            menu.AddSubMenu(targetSelector);

            // Orbwalker
            Menu orbwalker = new Menu("Orbwalker", "orbwalker");
            OW = new Orbwalking.Orbwalker(orbwalker);
            menu.AddSubMenu(orbwalker);

            // Combo
            Menu combo = new Menu("Combo", "combo");
            combo.AddItem(new MenuItem("comboUseQ", "Use Q").SetValue(true));
            combo.AddItem(new MenuItem("comboUseE", "Use E").SetValue(true));
            combo.AddItem(new MenuItem("comboNumE", "Stacks for E").SetValue(new Slider(5, 1, 20)));
            combo.AddItem(new MenuItem("comboUseItems", "Use items").SetValue(true));
            combo.AddItem(new MenuItem("comboUseIgnite", "Use Ignite").SetValue(true));
            combo.AddItem(new MenuItem("comboActive", "Combo active").SetValue(new KeyBind(32, KeyBindType.Press)));
            menu.AddSubMenu(combo);

            // Harass
            Menu harass = new Menu("Harass", "harass");
            harass.AddItem(new MenuItem("harassUseQ", "Use Q").SetValue(true));
            harass.AddItem(new MenuItem("harassMana", "Mana usage in percent (%)").SetValue(new Slider(30)));
            harass.AddItem(new MenuItem("harassActive", "Harass active").SetValue(new KeyBind('C', KeyBindType.Press)));
            menu.AddSubMenu(harass);

            // WaveClear
            Menu waveClear = new Menu("WaveClear", "waveClear");
            waveClear.AddItem(new MenuItem("waveUseE", "Use E").SetValue(true));
            waveClear.AddItem(new MenuItem("waveNumE", "Minion kill number for E").SetValue(new Slider(2, 1, 10)));
            waveClear.AddItem(new MenuItem("waveMana", "Mana usage in percent (%)").SetValue(new Slider(30)));
            waveClear.AddItem(new MenuItem("waveActive", "WaveClear active").SetValue(new KeyBind('V', KeyBindType.Press)));
            menu.AddSubMenu(waveClear);

            // JungleClear
            Menu jungleClear = new Menu("JungleClear", "jungleClear");
            jungleClear.AddItem(new MenuItem("jungleUseE", "Use E").SetValue(true));
            jungleClear.AddItem(new MenuItem("jungleActive", "JungleClear active").SetValue(new KeyBind('V', KeyBindType.Press)));
            menu.AddSubMenu(jungleClear);

            // Flee
            Menu flee = new Menu("Flee", "flee");
            //flee.AddItem(new MenuItem("fleeWalljump", "Try to jump over walls").SetValue(true));
            flee.AddItem(new MenuItem("fleeAA", "Smart usage of AA").SetValue(true));
            flee.AddItem(new MenuItem("fleeActive", "Flee active").SetValue(new KeyBind('T', KeyBindType.Press)));
            menu.AddSubMenu(flee);

            // Misc
            Menu misc = new Menu("Misc", "misc");
            misc.AddItem(new MenuItem("miscKillstealE", "Killsteal with E").SetValue(true));
            menu.AddSubMenu(misc);

            // Items
            Menu items = new Menu("Items", "items");
            items.AddItem(new MenuItem("itemsBotrk", "Use BotRK").SetValue(true));
            menu.AddSubMenu(items);

            // Drawings
            Menu drawings = new Menu("Drawings", "drawings");
            drawings.AddItem(new MenuItem("drawRangeQ", "Q range").SetValue(new Circle(true, Color.FromArgb(150, Color.IndianRed))));
            drawings.AddItem(new MenuItem("drawRangeW", "W range").SetValue(new Circle(false, Color.FromArgb(150, Color.MediumPurple))));
            drawings.AddItem(new MenuItem("drawRangeE", "E range").SetValue(new Circle(true, Color.FromArgb(150, Color.DarkRed))));
            drawings.AddItem(new MenuItem("drawRangeR", "R range").SetValue(new Circle(false, Color.FromArgb(150, Color.Red))));
            menu.AddSubMenu(drawings);

            // Finalize menu
            menu.AddToMainMenu();
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

        public static void ManaMenager()
        {

            QMANA = 45 + 5 * Q.Level;
            WMANA = 25;
            EMANA = 35 * CountEnemies(ObjectManager.Player, 2000);
            if (!R.IsReady())
                RMANA = EMANA - 10;
            else
                RMANA = 100;
        }
    }
}