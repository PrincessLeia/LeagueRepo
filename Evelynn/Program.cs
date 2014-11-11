#region

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

#endregion

namespace Evelynn
{
    internal class Program
    {
        public const string ChampionName = "Evelynn";

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;

        //Spells
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        //Menu
        public static Menu Config;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName != ChampionName) return;

            //Create the spells
            Q = new Spell(SpellSlot.Q, 500f);
            W = new Spell(SpellSlot.W, Q.Range);
            E = new Spell(SpellSlot.E, 225f + 2 * 65f);
            R = new Spell(SpellSlot.R, 650f);

            R.SetSkillshot(0.25f, 300f, float.MaxValue, false, SkillshotType.SkillshotCircle);
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

            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            Config.SubMenu("Combo")
                .AddItem(
                    new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind("A".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLaneClear", "Use Q").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseELaneClear", "Use E").SetValue(true));
            Config.SubMenu("LaneClear")
                .AddItem(
                    new MenuItem("LaneClearActive", "JungleFarm!").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));

            Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));
            Config.SubMenu("JungleFarm")
                .AddItem(
                    new MenuItem("JungleFarmActive", "JungleFarm!").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("ERange", "E range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("RRange", "R range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));

            Config.AddToMainMenu();

            //Add the events we are going to use:
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active)
                    Utility.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (!Orbwalking.CanMove(40)) return;

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
                return;
            }

            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active && ObjectManager.Player.Mana > ObjectManager.Player.MaxMana * 0.2)
                LaneClear();

            if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active && ObjectManager.Player.Mana > ObjectManager.Player.MaxMana * 0.2)
                JungleFarm();
        }



        private static void Combo()
        {
            var target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.True);
            var target2 = SimpleTs.GetTarget(720f, SimpleTs.DamageType.True);
            if (target2 != null)
            {
                if (Config.Item("UseWCombo").GetValue<bool>() && W.IsReady())
                    W.Cast();

                if (Config.Item("UseRCombo").GetValue<bool>() && R.IsReady())
                {
                    if (CountEnemies(target2, 400f) > 1)
                        R.Cast(target, true, true);
                    if (ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.7)
                        R.Cast(target, true, false);
                }
            }
            if (target != null)
            {
                if (Config.Item("UseQCombo").GetValue<bool>())
                    Q.Cast();

                if (Config.Item("UseECombo").GetValue<bool>() && E.IsReady())
                    E.CastOnUnit(target,true);
            }
        }

        private static void JungleFarm()
        {
            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range,
                MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (mobs.Count > 0)
            {
                if (Config.Item("UseQJFarm").GetValue<bool>() && Q.IsReady())
                    Q.Cast();

                if (Config.Item("UseEJFarm").GetValue<bool>() && E.IsReady())
                    E.CastOnUnit(mobs[0]);
            }
        }

        private static void LaneClear()
        {
            var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range);

            foreach (var minion in minions.Where(minion => minion.IsValidTarget(Q.Range)))
            {
                if (Config.Item("UseQLaneClear").GetValue<bool>() && Q.IsReady())
                    Q.Cast();

                if (Config.Item("UseELaneClear").GetValue<bool>() && E.IsReady())
                    E.CastOnUnit(minion);
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
    }
}