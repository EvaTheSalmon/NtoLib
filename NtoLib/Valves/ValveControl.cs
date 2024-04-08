﻿using FB.VisualFB;
using InSAT.Library.Gui;
using InSAT.Library.Interop.Win32;
using NtoLib.Utils;
using NtoLib.Valves.Render;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NtoLib.Valves
{
    [ComVisible(true)]
    [Guid("09DF2B43-AE07-4A93-9670-FE1ED79E0751")]
    [DisplayName("Имя какое-то")]
    public partial class ValveControl : VisualControlBase
    {
        private float _penWidth = 2f;
        [DisplayName("Толщина линии")]
        public float PenWidth
        {
            get
            {
                return _penWidth;
            }
            set
            {
                if(value < 1)
                    _penWidth = 1;
                else
                    _penWidth = value;
            }
        }

        private float _errorPenWidth = 2f;
        [DisplayName("Толщина линии ошибки")]
        public float ErrorPenWidth
        {
            get
            {
                return _errorPenWidth;
            }
            set
            {
                if(value < 1)
                    _errorPenWidth = 1;
                else
                    _errorPenWidth = value;
            }
        }

        private float _errorOffset = 5f;
        [DisplayName("Отступ от ошибки")]
        public float ErrorOffset
        {
            get
            {
                return _errorOffset;
            }
            set
            {
                if(value < 0)
                    _errorOffset = 0;
                else
                    _errorOffset = value;
            }
        }

        [DisplayName("Ориентация")]
        public Orientation Orientation { get; set; }

        [DisplayName("Шибер")]
        public bool IsSlideGate { get; set; }


        internal State State;
        private bool _commandImpulseInProgress;

        private SettingsForm _settingsForm;
        private Blinker _blinker;



        public ValveControl()
        {
            InitializeComponent();

            BackColor = Color.WhiteSmoke;

            _blinker = new Blinker(500);
            _blinker.OnLightChanged += InvalidateIfNeeded;
        }



        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);

            if(!FBConnector.DesignMode)
                UpdateState();

            PaintData paintData = new PaintData {
                Bounds = Bounds,
                LineWidth = PenWidth,
                ErrorLineWidth = ErrorPenWidth,
                ErrorOffset = ErrorOffset,
                Orientation = Orientation,
                Blinker = _blinker
            };


            BaseRenderer renderer = new CommonValveRenderer();
            renderer.Paint(e.Graphics, paintData, State);
        }


        private void OnClick(object sender, EventArgs e)
        {
            MouseEventArgs me = (MouseEventArgs)e;
            if(me.Button != MouseButtons.Right)
                return;

            if(_settingsForm == null)
                OpenSettings();
        }

        private void OnDoubleClick(object sender, EventArgs e)
        {
            MouseEventArgs me = (MouseEventArgs)e;
            if(me.Button != MouseButtons.Left || _commandImpulseInProgress)
                return;


            int commandId;
            if(State.Closed)
                commandId = ValveFB.OpenCMD;
            else if(State.Opened)
                commandId = ValveFB.CloseCMD;
            else
                return;

            SetPinValue(commandId, true);

            Task.Run(() => SendCommandImpulse(commandId, 500));
        }


        private void OnVisibleChanged(object sender, EventArgs e)
        {
            if(!Visible)
                _settingsForm?.Close();
        }



        private async Task SendCommandImpulse(int outputId, int msDuration)
        {
            SetPinValue(outputId, true);
            _commandImpulseInProgress = true;

            await Task.Delay(msDuration);

            SetPinValue(outputId, false);
            _commandImpulseInProgress = false;
        }

        private void InvalidateIfNeeded()
        {
            if(State.Error)
                Invalidate();
        }

        private void OpenSettings()
        {
            _settingsForm = new SettingsForm(this);
            _settingsForm.FormClosed += OnSettingsFormClosed;
            _settingsForm.Show(Win32Window.FromInt32(User32.GetParent(Handle)));
        }

        private void OnSettingsFormClosed(object sender, FormClosedEventArgs e)
        {
            SettingsForm form = (SettingsForm)sender;
            form.FormClosed -= OnSettingsFormClosed;
            _settingsForm = null;
        }

        private void UpdateState()
        {
            State.ConnectionOk = GetPinValue<bool>(ValveFB.ConnectionOk);
            State.Opened = GetPinValue<bool>(ValveFB.Opened);
            State.Closed = GetPinValue<bool>(ValveFB.Closed);
            State.Error = GetPinValue<bool>(ValveFB.Error);
            State.OldError = GetPinValue<bool>(ValveFB.OldError);
            State.BlockOpening = GetPinValue<bool>(ValveFB.BlockOpening);
            State.BlockClosing = GetPinValue<bool>(ValveFB.BlockClosing);
            State.AutoMode = GetPinValue<bool>(ValveFB.AutoMode);
            State.Collision = GetPinValue<bool>(ValveFB.Collision);

            _settingsForm?.Invalidate();
        }

        private T GetPinValue<T>(int id)
        {
            return FBConnector.GetPinValue<T>(id + 1000);
        }

        private void SetPinValue<T>(int id, T value)
        {
            FBConnector.SetPinValue(id + 1000, value);
        }
    }
}