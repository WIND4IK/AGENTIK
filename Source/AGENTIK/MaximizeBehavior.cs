using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Huddled.Interop;
using Huddled.Interop.Windows;
using Huddled.Wpf;
using MessageMapping = System.Collections.Generic.KeyValuePair<Huddled.Interop.NativeMethods.WindowMessage, Huddled.Interop.NativeMethods.MessageHandler>;

namespace AGENTIK
{
    public class MaximizeBehavior : NativeBehavior
    {
        private bool _maximizeCommandRecieved;

        protected override IEnumerable<MessageMapping> Handlers
        {
            get
            {
                yield return new MessageMapping(NativeMethods.WindowMessage.WindowPositionChanging, OnPreviewPositionChange);
                yield return new MessageMapping(NativeMethods.WindowMessage.SysCommand, OnSysCommand);
                yield return new MessageMapping(NativeMethods.WindowMessage.SysChar, OnGetMinMaxInfo);
            }
        }

        private IntPtr OnPreviewPositionChange(IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (_maximizeCommandRecieved)
            {
                var windowPosition = (NativeMethods.WindowPosition)Marshal.PtrToStructure(lParam, typeof(NativeMethods.WindowPosition));

                // If we use the WPF SystemParameters, these should be "Logical" pixels
                var source = PresentationSource.FromVisual(AssociatedObject);
                // MUST use the position from the lParam, NOT the current position of the AssociatedObject
                var localWorkAreaRect = windowPosition.GetLocalWorkAreaRect();
                Rect validArea = localWorkAreaRect.DPITransformFromWindow(source);

                var newWindowPosition = AssociatedObject.RestoreBounds.DPITransformToWindow(source);
                
                windowPosition.Left = newWindowPosition.Left;
                windowPosition.Width = newWindowPosition.Width;
                windowPosition.Height = localWorkAreaRect.Height;

                _maximizeCommandRecieved = false;
                Marshal.StructureToPtr(windowPosition, lParam, true);
            }
            return IntPtr.Zero;
        }

        private IntPtr OnSysCommand(IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            _maximizeCommandRecieved = wParam.ToInt32() == (int) NativeMethods.SystemMenuItem.Maximize;
            return IntPtr.Zero;
        }

        private IntPtr OnGetMinMaxInfo(IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            //For DevExpress DXRibbonWindow lParam = 122413148 on miximazed
            _maximizeCommandRecieved = wParam.ToInt32() == (int)2;
            if(false)
                _maximizeCommandRecieved = true;
            return IntPtr.Zero;
        }
    }
}
