using System;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace BorderlessGraphicViewer
{
    public class PreviewMouseMiddleButtonDown : EventTrigger
    {
        public PreviewMouseMiddleButtonDown()
        {
            EventName = "PreviewMouseDown";
        }

        protected override void OnEvent(EventArgs eventArgs)
        {
            var mbea = eventArgs as MouseButtonEventArgs;
            if (mbea != null && mbea.ChangedButton == MouseButton.Middle)
                base.OnEvent(eventArgs);
        }
    }
}