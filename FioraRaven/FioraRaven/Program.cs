using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

using Color = System.Drawing.Color;

namespace FioraRaven
{
    class Fiora
    {
        public static String champName = "Fiora";
        public static Orbwalking.Orbwalker Orbwalker;
        public static Obj_AI_Base player = ObjectManager.Player;
        public static Spell Q, W, E, R;
        public static Menu menu;
        public static Obj_AI_Hero tar;
        public static Dictionary<string, SpellSlot> spellData;
        public static DZApi api = new DZApi();
        public static bool firstQ;
        public static float QCastTime;
        public static int QMANA;
        public static int WMANA;
        public static int EMANA;
        public static int RMANA;
        static void Main(string[] args)
        {
            try
            {
                CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return;
            }
        }
        static void Game_OnGameLoad(EventArgs args)
        {
            if (player.BaseSkinName != champName) return;
            spellData = new Dictionary<string, SpellSlot>();
            menu = new Menu("Fiora Raven","FioraRMenu",true);
            menu.AddSubMenu(new Menu("Orbwalker","Orbwalker1"));
            Orbwalker = new Orbwalking.Orbwalker(menu.SubMenu("Orbwalker1"));
            var ts = new Menu("Target Selector","TargetSelector");
            SimpleTs.AddToMenu(ts);
            menu.AddSubMenu(ts);
            menu.AddSubMenu(new Menu("Fiora Combo", "Combo"));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseQ", "Use Q").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseW", "Use W").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseE", "Use E").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseR", "Use R").SetValue(true));
            menu.AddSubMenu(new Menu("Fiora Misc", "Misc"));
            menu.SubMenu("Misc").AddItem(new MenuItem("WBlock", "Use W to block Autos").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("RDodge", "Use R to dodge dangerous").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("SecondQDelay", "Second Q Delay (ms)").SetValue(new Slider(650, 0, 4000)));
            menu.AddSubMenu(new Menu("Fiora Items", "Item"));
            menu.SubMenu("Item").AddItem(new MenuItem("Botrk", "Use Botrk").SetValue(true));
            menu.SubMenu("Item").AddItem(new MenuItem("Youmuu", "Use Youmuu").SetValue(true));
            menu.SubMenu("Item").AddItem(new MenuItem("Tiamat", "Use Tiamat").SetValue(true));
            menu.SubMenu("Item").AddItem(new MenuItem("Hydra", "Use Hydra").SetValue(true));
            menu.SubMenu("Item").AddItem(new MenuItem("OwnHPercBotrk", "Min Own H % Botrk").SetValue(new Slider(50, 1, 100)));
            menu.SubMenu("Item").AddItem(new MenuItem("EnHPercBotrk", "Min Enemy H % Botrk").SetValue(new Slider(20, 1, 100)));
            menu.SubMenu("Item").AddItem(new MenuItem("ItInComb", "Use Items In Combo").SetValue(true));
            menu.AddSubMenu(new Menu("Fiora Dangerous Spells", "DangSpells"));
            Dictionary<String, String> dSpellsDName = api.getDanSpellsName();
            foreach (KeyValuePair<string, string> entry in dSpellsDName)
            {
                menu.SubMenu("DangSpells").AddItem(new MenuItem(entry.Key, entry.Value).SetValue(true));
            }
            menu.AddSubMenu(new Menu("Fiora Drawing", "Drawing"));
            menu.SubMenu("Drawing").AddItem(new MenuItem("DrQ", "Draw Q").SetValue(true));
            menu.SubMenu("Drawing").AddItem(new MenuItem("DrR", "Draw R").SetValue(true));
           
            Game.PrintChat("Fiora The Raven By DZ191 Loaded ");
            menu.AddToMainMenu();
            Obj_AI_Base.OnProcessSpellCast += Game_ProcessSpell;
            Game.OnGameUpdate += Game_OnGameUpdate;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            Orbwalking.OnAttack += Orbwalking_OnAttack;
            Drawing.OnDraw += Drawing_OnDraw;
            Q = new Spell(SpellSlot.Q, 600f);
            W = new Spell(SpellSlot.W, float.MaxValue);
            E = new Spell(SpellSlot.E, float.MaxValue);
            R = new Spell(SpellSlot.R, 400f);
            
        }
        static void Drawing_OnDraw(EventArgs args)
        {
            if (isEn("DrQ"))
            {
                Utility.DrawCircle(player.Position, Q.Range, Color.MediumPurple);
            }
            if (isEn("DrR"))
            {
                Utility.DrawCircle(player.Position, R.Range, Color.MediumPurple);
            }
        }
       
        public static void Game_ProcessSpell(Obj_AI_Base hero, GameObjectProcessSpellCastEventArgs args)
        {
            String name = args.SData.Name;
            Obj_AI_Hero tar = (Obj_AI_Hero)hero;
            GameObjectProcessSpellCastEventArgs spell = args;
            if(api.getDanSpellsName().ContainsKey(args.SData.Name) && isEn(name))
            {
                //Got Dangerous Spell. Starting Predictions and Custom Evade Logics.
                if(name == "CurseofTheSadMummy")
                {
                    if(player.Distance(hero.Position)<=600f)
                    {
                        Obj_AI_Hero tar1 = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Physical);
                        R.Cast(tar1);
                    }
                }
                if(name == "InfernalGuardian" || name == "UFSlash")
                {
                    if (player.Distance(spell.End)<=270f)
                    {
                        Obj_AI_Hero tar1 = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Physical);
                        R.Cast(tar1);
                    }
                }
                if (name == "BlindMonkRKick" || name == "syndrar" || name == "VeigarPrimordialBurst" || name == "AlZaharNetherGrasp")
                {
                    if (spell.Target.IsMe)
                    {
                        Obj_AI_Hero tar1 = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Physical);
                        R.Cast(tar1);
                    }
                }
                if (name == "BusterShot" || name == "ViR")
                {
                    if (spell.Target.IsMe || player.Distance(spell.Target.Position)<=50f)
                    {
                        Obj_AI_Hero tar1 = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Physical);
                        R.Cast(tar1);
                    }
                }
                
                if (name == "GalioIdolOfDurand")
                {
                    if (player.Distance(hero.Position) <= 600f)
                    {
                        Obj_AI_Hero tar1 = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Physical);
                        R.Cast(tar1);
                    }
                }
            }
            if (spell.SData.Name.Contains("Attack") && isEn("WBlock") && spell.Target.IsMe && (ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.4 && ObjectManager.Player.Mana > RMANA + RMANA + QMANA + WMANA + EMANA))
            {
                    W.Cast();      
            }
        }
        static void Orbwalking_OnAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            if (unit.IsMe)
            {
                tar = (Obj_AI_Hero)target;
                if (isEn("Botrk") && isCombo() && target.IsValidTarget()&&isEn("ItInComb"))
                {
                    float OwnH = api.getPlHPer();
                    if ((menu.Item("OwnHPercBotrk").GetValue<Slider>().Value <= OwnH) && ((menu.Item("EnHPercBotrk").GetValue<Slider>().Value <= api.getEnH(tar))))
                    {
                        api.useItem(3153, tar);
                    }
                }
                if (isEn("Youmuu") && isCombo() && target.IsValidTarget() && isEn("ItInComb"))
                {
                    api.useItem(3142, tar);
                }
            }
        }
        public static void Orbwalking_AfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            if (unit.IsMe)
            {
                tar = (Obj_AI_Hero)target;
                
                if (menu.Item("Tiamat").GetValue<bool>() && isCombo())
                {
                    int itemId = 3077;
                    api.useItem(itemId);
                }
                if (menu.Item("Hydra").GetValue<bool>() && isCombo())
                {
                    int itemId = 3074;
                    api.useItem(itemId);
                }
                firstQ = false;
            }
        }
        public static void Game_OnGameUpdate(EventArgs args)
        {
            if ((Game.Time - QCastTime) >= 4) firstQ = false;
            
            if (isCombo())
            { 
                CastQ();
            }
            
            if (R.IsReady())
            {
                foreach (var target2 in ObjectManager.Get<Obj_AI_Hero>().Where(target2 => target2.IsValidTarget(R.Range)))
                {
                    if ((R.GetDamage(target2) >= target2.Health))
                    {
                        R.Cast(target2, true);
                    }
                }
            }
        }
        public static void CastQ()
        {
            var target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);
            double closest = 10000000000000;
            Obj_AI_Minion candidate = new Obj_AI_Minion();
            if (target.IsValidTarget(Q.Range) && Q.IsReady() && Q.InRange(target.ServerPosition) && isEn("UseQ") && !firstQ && (ObjectManager.Player.Distance(target) > 300 || (Game.Time - QCastTime) >= (menu.Item("SecondQDelay").GetValue<Slider>().Value / 1000)))
            {
                Q.Cast(target, true, false);
                firstQ = true;
                QCastTime = Game.Time;
                if (E.IsReady() && ObjectManager.Player.Mana > RMANA + EMANA) E.Cast();
            }
            else
            {
                foreach (Obj_AI_Minion minion in ObjectManager.Get<Obj_AI_Minion>().Where(minion => minion.IsValid))
                {
                    if (minion.IsValid && Vector2.Distance(player.Position.To2D(), minion.Position.To2D()) < 600 && minion.IsEnemy && closest > Vector3.DistanceSquared(Game.CursorPos, minion.Position) )
                    {
                        candidate = minion;
                        closest = Vector3.DistanceSquared(Game.CursorPos, minion.Position);
                    }
                }
                if (candidate.IsValid && !target.IsValidTarget() && Q.IsReady() && Vector2.Distance(player.Position.To2D(), candidate.Position.To2D()) < 600 && Vector3.DistanceSquared(Game.CursorPos, candidate.Position) < Vector3.DistanceSquared(Game.CursorPos, ObjectManager.Player.Position) && ((Game.Time - QCastTime) >= (menu.Item("SecondQDelay").GetValue<Slider>().Value / 1000) || !firstQ))
                {
                    Q.Cast(candidate, true, false);
                    firstQ = true;
                    QCastTime = Game.Time;
                }
            }

        }
        public static void CastR(Obj_AI_Hero target)
        {
            if (target.IsValidTarget() && R.InRange(target.ServerPosition) && isEn("UseR"))
            {
                R.Cast(target,true);
            }
        }
        public static bool isCombo()
        {
            return Orbwalker.ActiveMode.ToString() == "Combo";
        }
        public static bool isEn(String item)
        {
            return menu.Item(item).GetValue<bool>();
        }
        public static void ManaMenager()
        {
            QMANA = 60;
            WMANA = 45;
            EMANA = 55;
            if (!R.IsReady())
                RMANA = QMANA - 10;
            else
                RMANA = 100;
        }
        
    }
}
