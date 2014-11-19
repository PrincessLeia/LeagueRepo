using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
namespace Draven
{
    // ReSharper disable once InconsistentNaming
    class Program
    {
        
        public const string ChampionName = "Draven";

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;

        //Spells
        public static List<Axe> AxeList = new List<Axe>();

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

        public static void Game_OnGameLoad(EventArgs args)
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 1100);
            E.SetSkillshot(250f, 130f, 1400f, false, SkillshotType.SkillshotLine);
            R = new Spell(SpellSlot.R, 20000);
            R.SetSkillshot(400f, 160f, 2000f, false, SkillshotType.SkillshotLine);

            //Create the menu
            Config = new Menu(ChampionName, ChampionName, true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            //Orbwalker submenu
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));

            //Load the orbwalker and add it to the submenu.
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            LoadMenu();
            Config.AddToMainMenu();

            GameObject.OnCreate += OnCreateObject;
            GameObject.OnDelete += OnDeleteObject;
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPosibleToInterrupt;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
        }

        public static void LoadMenu()
        {
            Config.AddSubMenu(new Menu("Combo", ObjectManager.Player.ChampionName + "Combo"));
            Config.SubMenu(ObjectManager.Player.ChampionName + "Combo").AddItem(new MenuItem("sep0", "====== Combo"));
            Config.SubMenu(ObjectManager.Player.ChampionName + "Combo").AddItem(new MenuItem("useQ_Combo", "= Use Q").SetValue(true));
            Config.SubMenu(ObjectManager.Player.ChampionName + "Combo").AddItem(new MenuItem("useW_Combo", "= Use W").SetValue(false));
            Config.SubMenu(ObjectManager.Player.ChampionName + "Combo").AddItem(new MenuItem("useE_Combo", "= Use E").SetValue(false));
            Config.SubMenu(ObjectManager.Player.ChampionName + "Combo").AddItem(new MenuItem("sep1", "========="));

            Config.AddSubMenu(new Menu("Harass", ObjectManager.Player.ChampionName + "Harass"));
            Config.SubMenu(ObjectManager.Player.ChampionName + "Harass").AddItem(new MenuItem("sep0", "====== Harass"));

            Config.SubMenu(ObjectManager.Player.ChampionName + "Harass").AddItem(new MenuItem("useQ_Harass", "= Use Q").SetValue(true));
            Config.SubMenu(ObjectManager.Player.ChampionName + "Harass").AddItem(new MenuItem("useW_Harass", "= Use W").SetValue(false));
            Config.SubMenu(ObjectManager.Player.ChampionName + "Harass").AddItem(new MenuItem("sep1", "========="));

            Config.AddSubMenu(new Menu("LaneClear", ObjectManager.Player.ChampionName + "LaneClear"));
            Config.SubMenu(ObjectManager.Player.ChampionName + "LaneClear").AddItem(new MenuItem("sep0", "====== LaneClear"));
            Config.SubMenu(ObjectManager.Player.ChampionName + "LaneClear").AddItem(new MenuItem("useQ_LaneClear", "= Use Q").SetValue(true));
            Config.SubMenu(ObjectManager.Player.ChampionName + "LaneClear").AddItem(new MenuItem("useW_LaneClear", "= Use W").SetValue(false));
            Config.SubMenu(ObjectManager.Player.ChampionName + "LaneClear").AddItem(new MenuItem("useE_LaneClear", "= Use E").SetValue(true));
            Config.SubMenu(ObjectManager.Player.ChampionName + "LaneClear").AddItem(new MenuItem("sep1", "========="));

            //Config.AddSubMenu(new Menu("RunLikeHell", ObjectManager.Player.ChampionName + "RunLikeHell"));
            //Config.SubMenu(ObjectManager.Player.ChampionName + "RunLikeHell").AddItem(new MenuItem("sep0", "====== RunLikeHell"));
            //Config.SubMenu(ObjectManager.Player.ChampionName + "RunLikeHell").AddItem(new MenuItem("useW_RunLikeHell", "= W to speed up").SetValue(true));
            //Config.SubMenu(ObjectManager.Player.ChampionName + "RunLikeHell").AddItem(new MenuItem("sep1", "========="));

            Config.AddSubMenu(new Menu("Misc", ObjectManager.Player.ChampionName + "Misc"));
            Config.SubMenu(ObjectManager.Player.ChampionName + "Misc").AddItem(new MenuItem("sep0", "====== Misc"));
            Config.SubMenu(ObjectManager.Player.ChampionName + "Misc").AddItem(new MenuItem("useE_Interrupt", "= E to Interrupt").SetValue(true));
            Config.SubMenu(ObjectManager.Player.ChampionName + "Misc").AddItem(new MenuItem("useE_GapCloser", "= E Anti Gapclose").SetValue(true));
            Config.SubMenu(ObjectManager.Player.ChampionName + "Misc").AddItem(new MenuItem("useCatchAxe_Combo", "= Combo CatchAxeRange").SetValue(new Slider(300, 0, 1000)));
            Config.SubMenu(ObjectManager.Player.ChampionName + "Misc").AddItem(new MenuItem("useCatchAxe_Harass", "= Harass CatchAxeRange").SetValue(new Slider(400, 0, 1000)));
            Config.SubMenu(ObjectManager.Player.ChampionName + "Misc").AddItem(new MenuItem("useCatchAxe_LaneClear", "= LaneClear CatchAxeRange").SetValue(new Slider(700, 0, 1000)));
            Config.SubMenu(ObjectManager.Player.ChampionName + "Misc").AddItem(new MenuItem("useCatchAxe_Lasthit", "= Lasthit CatchAxeRange").SetValue(new Slider(500, 0, 1000)));
            Config.SubMenu(ObjectManager.Player.ChampionName + "Misc").AddItem(new MenuItem("useW_SpeecBuffCatch", "= Use W to Catch Axes").SetValue(true));
            Config.SubMenu(ObjectManager.Player.ChampionName + "Misc").AddItem(new MenuItem("sep1", "========="));

            Config.AddSubMenu(new Menu("Drawing", ObjectManager.Player.ChampionName + "Drawing"));
            Config.SubMenu(ObjectManager.Player.ChampionName + "Drawing").AddItem(new MenuItem("sep0", "====== Drawing"));
            Config.SubMenu(ObjectManager.Player.ChampionName + "Drawing").AddItem(new MenuItem("Draw_Disabled", "Disable All").SetValue(false));
            Config.SubMenu(ObjectManager.Player.ChampionName + "Drawing").AddItem(new MenuItem("Draw_E", "Draw E").SetValue(true));
            Config.SubMenu(ObjectManager.Player.ChampionName + "Drawing").AddItem(new MenuItem("Draw_CatchRange", "Draw CatchRange").SetValue(true));
        }

        public static void OnDraw(EventArgs args)
        {

            if (Config.Item("Draw_Disabled").GetValue<bool>())
                return;

            if (Config.Item("Draw_CatchRange").GetValue<bool>())
                if (Q.Level > 0)
                {
                        if (Orbwalker.ActiveMode.ToString() == "Combo")
                            Utility.DrawCircle(Game.CursorPos, Config.Item("useCatchAxe_Combo").GetValue<Slider>().Value, Color.Blue);
                        if (Orbwalker.ActiveMode.ToString() == "Mixed")
                            Utility.DrawCircle(Game.CursorPos, Config.Item("useCatchAxe_Harass").GetValue<Slider>().Value, Color.Blue);
                        if (Orbwalker.ActiveMode.ToString() == "Combo")
                            Utility.DrawCircle(Game.CursorPos, Config.Item("useCatchAxe_LaneClear").GetValue<Slider>().Value, Color.Blue);
                }

        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            CatchAxe();
            switch (Orbwalker.CurrentMode)
            {
                case Orbwalker.Mode.Combo:
                    if (Config.Item("useQ_Combo").GetValue<bool>())
                        CastQ();
                    if (Config.Item("useW_Combo").GetValue<bool>())
                        CastW();
                    if (Config.Item("useE_Combo").GetValue<bool>())
                        Cast_BasicSkillshot_Enemy(E);
                    break;
                case Orbwalker.Mode.Harass:
                    if (Config.Item("useQ_Harass").GetValue<bool>() && ManamanagerAllowCast("ManaManager_Harass"))
                        CastQ();
                    if (Config.Item("useW_Harass").GetValue<bool>() && ManamanagerAllowCast("ManaManager_Harass"))
                        CastW();
                    break;
                case Orbwalker.Mode.LaneClear:
                    if (Config.Item("useQ_LaneClear").GetValue<bool>() && ManamanagerAllowCast("ManaManager_LaneClear"))
                        CastQ();
                    if (Config.Item("useW_LaneClear").GetValue<bool>() && ManamanagerAllowCast("ManaManager_LaneClear"))
                        CastW();
                    if (Config.Item("useE_LaneClear").GetValue<bool>() && ManamanagerAllowCast("ManaManager_LaneClear"))
                        Cast_BasicSkillshot_Enemy(E);
                    break;
            }
        }

        public static void CatchAxe()
        {
            if (AxeList.Count > 0)
            {
                Axe[] axe = { null };
                foreach (var obj in AxeList.Where(obj => axe[0] == null || obj.CreationTime < axe[0].CreationTime))
                    axe[0] = obj;
                if (axe[0] != null)
                {
                    var distanceNorm = Vector2.Distance(axe[0].Position.To2D(), ObjectManager.Player.ServerPosition.To2D()) - ObjectManager.Player.BoundingRadius;
                    var distanceBuffed = ObjectManager.Player.GetPath(axe[0].Position).ToList().To2D().PathLength();
                    var canCatchAxeNorm = distanceNorm / ObjectManager.Player.MoveSpeed + Game.Time < axe[0].EndTime;
                    var canCatchAxeBuffed = distanceBuffed / (ObjectManager.Player.MoveSpeed + (5 * W.Level + 35) * 0.01 * ObjectManager.Player.MoveSpeed + Game.Time) < axe[0].EndTime;

                    if (!Config.Item("useW_SpeecBuffCatch").GetValue<bool>())
                        if (!canCatchAxeNorm)
                        {
                            AxeList.Remove(axe[0]);
                            return;
                        }

                    if ((!(axe[0].Position.Distance(Game.CursorPos) < Config.Item("useCatchAxe_Combo").GetValue<Slider>().Value) ||
                         Orbwalker.CurrentMode != Orbwalker.Mode.Combo) &&
                        (!(axe[0].Position.Distance(Game.CursorPos) < Config.Item("useCatchAxe_Harass").GetValue<Slider>().Value) ||
                         Orbwalker.CurrentMode != Orbwalker.Mode.Harass) &&
                        (!(axe[0].Position.Distance(Game.CursorPos) < Config.Item("useCatchAxe_LaneClear").GetValue<Slider>().Value) ||
                         Orbwalker.CurrentMode != Orbwalker.Mode.LaneClear) &&
                        (!(axe[0].Position.Distance(Game.CursorPos) < Config.Item("useCatchAxe_Lasthit").GetValue<Slider>().Value) ||
                         Orbwalker.CurrentMode != Orbwalker.Mode.Lasthit))
                        return;
                    if (canCatchAxeBuffed && !canCatchAxeNorm && W.IsReady() && !axe[0].Catching())
                        W.Cast();
                    Orbwalker.CustomOrbwalkMode = true;
                    Orbwalker.Orbwalk(GetModifiedPosition(axe[0].Position, Game.CursorPos, 49 + ObjectManager.Player.BoundingRadius / 2), Orbwalker.GetPossibleTarget());
                }

            }
            else
                Orbwalker.CustomOrbwalkMode = false;
        }

        public static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!Config.Item("useE_GapCloser").GetValue<bool>())
                return;
            if (!(gapcloser.End.Distance(ObjectManager.Player.ServerPosition) <= 100))
                return;
            E.CastIfHitchanceEquals(gapcloser.Sender, HitChance.Medium, true);
        }

        public static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!Config.Item("useE_Interrupt").GetValue<bool>())
                return;
            E.CastIfHitchanceEquals(unit, HitChance.Medium, true);
        }

        private static void OnCreateObject(GameObject sender, EventArgs args)
        {
            if (!sender.Name.Contains("Q_reticle_self"))
                return;
            AxeList.Add(new Axe(sender));
        }

        private static void OnDeleteObject(GameObject sender, EventArgs args)
        {
            if (!sender.Name.Contains("Q_reticle_self"))
                return;
            foreach (var axe in AxeList.Where(axe => axe.NetworkId == sender.NetworkId))
            {
                AxeList.Remove(axe);
                return;
            }
        }

        private void CastQ()
        {
            if (!Q.IsReady())
                return;
            if (GetQStacks() > 0 || AxeList.Count > 2)
                return;
            var target = TargetSelector.GetAATarget();
            if (target != null)
                Q.Cast();

            if (Orbwalker.CurrentMode != Orbwalker.Mode.LaneClear)
                return;
            var allMinion = MinionManager.GetMinions(ObjectManager.Player.Position,
                Orbwalker.GetAutoAttackRangeto(), MinionTypes.All, MinionTeam.NotAlly);
            if (!allMinion.Any(minion => minion.IsValidTarget(Orbwalker.GetAutoAttackRangeto(minion))))
                return;
            Q.Cast();
        }

        private static void CastW()
        {
            if (!W.IsReady())
                return;

            var target = TargetSelector.GetAATarget();
            if (target != null)
                W.Cast();

            if (Orbwalker.CurrentMode != Orbwalker.Mode.LaneClear)
                return;
            var allMinion = MinionManager.GetMinions(ObjectManager.Player.Position,
                Orbwalker.GetAutoAttackRangeto(), MinionTypes.All, MinionTeam.NotAlly);
            if (!allMinion.Any(minion => minion.IsValidTarget(Orbwalker.GetAutoAttackRangeto(minion))))
                return;
            W.Cast();
        }

        public static int GetQStacks()
        {
            var buff = ObjectManager.Player.Buffs.FirstOrDefault(buff1 => buff1.Name.Equals("dravenspinningattack"));
            return buff != null ? buff.Count : 0;
        }

        internal class Axe
        {
            public GameObject AxeObject;
            public double CreationTime;
            public double EndTime;
            public int NetworkId;
            public Vector3 Position;

            public Axe(GameObject axeObject)
            {
                AxeObject = axeObject;
                NetworkId = axeObject.NetworkId;
                Position = axeObject.Position;
                CreationTime = Game.Time;
                EndTime = CreationTime + 1.2;
            }

            public float DistanceToPlayer()
            {
                return ObjectManager.Player.Distance(Position);
            }

            public bool Catching()
            {
                return ObjectManager.Player.Position.Distance(Position) <= 49 + ObjectManager.Player.BoundingRadius / 2 + 50;
            }
        }
    }
}