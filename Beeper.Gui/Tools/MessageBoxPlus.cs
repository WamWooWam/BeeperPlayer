using Beeper.Gui.Properties;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beeper.Gui.Tools
{
    class MessageBoxPlus
    {
        public static void ShowErrorDialog(Exception ex)
        {
            TaskDialog dialog = new TaskDialog();
            dialog.MainIcon = TaskDialogIcon.Error;
            dialog.WindowTitle = ex.GetType().ToString();
            dialog.MainInstruction = String.Format(Resources.ExceptionOccured, ex.GetType());
            dialog.Content = ex.Message;
            dialog.ExpandedByDefault = false;
            dialog.ExpandedControlText = Resources.HideStackTrace;
            dialog.CollapsedControlText = Resources.ShowStackTrace;
            dialog.ExpandedInformation = ex.StackTrace;
            dialog.Buttons.Add(new TaskDialogButton(ButtonType.Ok));
            dialog.Show();
        }
    }
}
