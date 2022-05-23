using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class GameComponent_TeleCore : GameComponent
    {
        private readonly ActionCompositionHandler actionCompositionHandler;
        private readonly TeleUpdateManager updateManager;

        public static GameComponent_TeleCore Instance()
        {
            return Current.Game.GetComponent<GameComponent_TeleCore>();
        }

        public GameComponent_TeleCore(Game game)
        {
            StaticData.Notify_Reload();
            actionCompositionHandler = new ActionCompositionHandler();
            updateManager = new TeleUpdateManager();
        }

        public override void ExposeData()
        {
            base.ExposeData();
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
        }

        public override void GameComponentTick()
        {
            actionCompositionHandler.TickActionComps();
            updateManager.Tick();
        }

        public override void GameComponentUpdate()
        {
            base.GameComponentUpdate();
        }

        public override void GameComponentOnGUI()
        {
            base.GameComponentOnGUI();
        }
    }
}
