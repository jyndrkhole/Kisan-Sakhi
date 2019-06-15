using System;
using UIKit;
using System.Collections.Generic;

namespace Chatter.iOS
{
    public class ActionController
    {
        public event EventHandler<EventArgs> SelectedAction;

        #region - Action Sheet for iOS 8

        public UIAlertController ShowActionSheetForiOS8(List<string> menuList, string actionTitleSheet)
        {
            UIAlertController controller = UIAlertController.Create(actionTitleSheet, null, UIAlertControllerStyle.ActionSheet);
            foreach (string name in menuList)
            {
                UIAlertAction alertAction = UIAlertAction.Create(name, UIAlertActionStyle.Default, alertActionHandler =>
                {
                    ActionSheetDelegateMethod(name);
                });
                controller.AddAction(alertAction);
            }
            return controller;
        }

        #endregion

        #region - Contact selection ActionSheet method

        public void ActionSheetDelegateMethod(string selectedMenu)
        {
            SelectedAction?.Invoke(selectedMenu, new EventArgs());
        }

        #endregion

        #region - Action Sheet for iOS Below 8

        public UIActionSheet ShowActionSheetForBelowiOS8(string[] menuItems)
        {
            UIActionSheet actionSheet = new UIActionSheet(null, null, "Cancel", null, menuItems);
            actionSheet.Clicked += (sender, e) =>
            {
                SelectedAction?.Invoke(sender, e);
            };
            return actionSheet;
        }

        #endregion
    }
}

