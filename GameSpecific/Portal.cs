using System;
using System.Diagnostics;
using System.Linq;
using LiveSplit.ComponentUtil;

namespace LiveSplit.SourceSplit.GameSpecific
{
    class Portal : GameSupport
    {
        // how to match this timing with demos:
        // start: crosshair appear
        // ending: crosshair disappear

        private bool _onceFlag;
        private const int VAULT_SAVE_TICK = 4261;
        private int _glados_index;

        public Portal()
        {
            this.GameTimingMethod = GameTimingMethod.EngineTicksWithPauses;
            this.AutoStartType = AutoStart.ViewEntityChanged;
            this.FirstMap = "testchmb_a_00";
            this.LastMap = "escape_02";
            // match portal demo timer
            this.StartOffsetTicks = 1;
            this.EndOffsetTicks = -1;
        }

        public override void OnSessionStart(GameState state)
        {
            base.OnSessionStart(state);

            if (this.IsLastMap && state.PlayerEntInfo.EntityPtr != IntPtr.Zero)
            {
                this._glados_index = state.GetEntIndexByName("glados_body");
                Debug.WriteLine("Glados index is " + this._glados_index);
            }
            _onceFlag = false;
        }

        public override GameSupportResult OnUpdate(GameState state)
        {
            if (this.IsFirstMap)
            {
                // vault save starts at tick 4261, but update interval may miss it so be a little lenient
                if ((state.TickBase >= VAULT_SAVE_TICK && state.TickBase <= VAULT_SAVE_TICK + 4) && !_onceFlag)
                {
                    _onceFlag = true;
                    int ticksSinceVaultSaveTick = state.TickBase - VAULT_SAVE_TICK; // account for missing ticks if update interval missed it
                    this.StartOffsetTicks = -3534 - ticksSinceVaultSaveTick; // 53.01 seconds
                    return GameSupportResult.PlayerGainedControl;
                }

                this.StartOffsetTicks = 1;
                return base.OnUpdate(state);
            }
            else if (!this.IsLastMap || _onceFlag)
                return GameSupportResult.DoNothing;

            if (this._glados_index != -1)
            {
                var newglados = state.GetEntInfoByIndex(_glados_index);

                if (newglados.EntityPtr == IntPtr.Zero)
                {
                    _glados_index = -1;
                    Debug.WriteLine("robot lady boom detected");
                    _onceFlag = true;
                    return GameSupportResult.PlayerLostControl;
                }
            }

            return GameSupportResult.DoNothing;
        }
    }
}
