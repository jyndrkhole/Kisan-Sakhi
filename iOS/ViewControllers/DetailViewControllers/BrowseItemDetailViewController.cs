using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Chatter.iOS.Database;
using Chatter.iOS.Utilities;
using CoreGraphics;
using Foundation;
using JSQMessagesViewController;
using SQLite;
using UIKit;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using static Chatter.iOS.Database.ChatterDatabase;
using AssetsLibrary;

namespace Chatter.iOS
{
    public partial class BrowseItemDetailViewController : MessagesViewController
    {
        public ItemsViewModel ViewModel { get; set; }
        public BrowseItemDetailViewController(IntPtr handle) : base(handle)
        {
        }

        MqttClient client;

        const string CLIENT_ID = "100";

        const string JAYENDRA = "Jayendra";
        const string JAYENDRA_ID = "2CC8343";
        const string CHATTER = "  सखी";
        const string CHATTER_ID = "BADB229";

        const string TOPIC_PUBLISH = "Jayendra";
        const string TOPIC_SUBSCRIBE = "Chatter";

        MessagesBubbleImage outgoingBubbleImageData, incomingBubbleImageData;
        List<Message> messages;

        ChatUser sender = new ChatUser { Id = JAYENDRA_ID, DisplayName = JAYENDRA };
        ChatUser friend = new ChatUser { Id = CHATTER_ID, DisplayName = CHATTER };

        UIImageView FriendImageView { set; get; }
        UILabel FriendNameLabel { set; get; }

        #region - UIViewController Life Cycle Methods

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            Title = string.Empty;

            ManageMessages();

            SenderId = sender.Id;
            SenderDisplayName = sender.DisplayName;


            var bubbleFactory = new MessagesBubbleImageFactory();

            outgoingBubbleImageData = bubbleFactory.CreateOutgoingMessagesBubbleImage(UIColorExtensions.MessageBubbleGreenColor);
            incomingBubbleImageData = bubbleFactory.CreateIncomingMessagesBubbleImage(UIColorExtensions.MessageBubbleGreenColor);

            CollectionView.CollectionViewLayout.IncomingAvatarViewSize = new CGSize(25, 25);
            CollectionView.CollectionViewLayout.OutgoingAvatarViewSize = CGSize.Empty;
        }

        bool isNavigatedForward;
        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            ConfigureTitleMenu();

            if (Reachability.IsNetworkAvailable())
            {
                InvokeInBackground(() =>
              {
                  if (!isNavigatedForward)
                      CreateMqttConnection();
              });
            }
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);

            DeallocViews();
        }

        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);

            if (client != null)
            {
                //client.Disconnect();
                client = null;
            }
        }

        #endregion

        #region - Subscribe Handlers

        public override async void PressedSendButton(UIButton button, string text, string senderId, string senderDisplayName, NSDate date)
        {
            if (!Reachability.IsNetworkAvailable())
            {
                new UIAlertView("Alert", "Please check your internet connection!", null, "Ok", null).Show();
                return;
            }

            SystemSoundPlayer.PlayMessageSentSound();

            if (client == null)
            {
                CreateMqttConnection();
            }

            client.Publish(TOPIC_PUBLISH, Encoding.UTF8.GetBytes(text), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, false);

            var message = new Message(SenderId, SenderDisplayName, NSDate.Now, text);
            if (message != null)
            {
                messages.Add(message);

                var conn = new SQLiteConnection(ChatterDatabase.DatabasePath());
                var s = conn.Insert(new ChatUserModel()
                {
                    Id = SenderId,
                    DisplayName = SenderDisplayName,
                    ChatDate = DateTime.Now,
                    Text = text
                });
            }

            FinishSendingMessage(true);

            await Task.Delay(500);
        }

        void ClientMqttMsgReceived(object sender, MqttMsgPublishEventArgs e)
        {
            UIApplication.SharedApplication.InvokeOnMainThread(async () =>
            {
                if (e.Message != null && !string.IsNullOrEmpty(Encoding.UTF8.GetString(e.Message)))
                {
                    ShowTypingIndicator = true;

                    ScrollToBottom(true);

                    var delay = Task.Delay(1500);

                    var message = new Message(friend.Id, friend.DisplayName, NSDate.Now, Encoding.UTF8.GetString(e.Message));
                    if (message != null)
                    {
                        messages.Add(message);
                    }

                    await delay;

                    ScrollToBottom(true);

                    SystemSoundPlayer.PlayMessageReceivedSound();

                    FinishReceivingMessage(true);

                    if (message != null)
                    {
                        var conn = new SQLiteConnection(ChatterDatabase.DatabasePath());
                        var s = conn.Insert(new ChatUserModel()
                        {
                            Id = message.SenderId,
                            DisplayName = friend.DisplayName,
                            ChatDate = DateTime.Now,
                            Text = Encoding.UTF8.GetString(e.Message)
                        });
                    }
                }
            });
        }

        #endregion

        #region - Configure UI Elements

        private void CreateMqttConnection()
        {
            client = new MqttClient("test.mosquitto.org");
            if (client != null)
            {
                try
                {
                    client.Connect(CLIENT_ID, null, null, false, 60);
                    client.Subscribe(new string[] { TOPIC_SUBSCRIBE }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
                    client.MqttMsgPublishReceived += ClientMqttMsgReceived;
                }
                catch (Exception e)
                {

                }
            }
        }

        private void ConfigureTitleMenu()
        {
            FriendImageView = new UIImageView(new CGRect(View.Bounds.Width / 2 - 10, 15, 32, 32));
            FriendImageView.Image = UIImage.FromBundle("friend");
            NavigationController.Add(FriendImageView);

            FriendNameLabel = new UILabel(new CGRect(View.Bounds.Width / 2 - 10, FriendImageView.Bounds.Bottom + 16, 300, 12));
            FriendNameLabel.TextAlignment = UITextAlignment.Left;
            FriendNameLabel.Text = CHATTER;
            FriendNameLabel.Font = UIFont.FromName("Helvetica-Bold", 12);
            NavigationController.Add(FriendNameLabel);
        }

        private void DeallocViews()
        {
            if (FriendImageView != null)
            {
                FriendImageView.RemoveFromSuperview();
                FriendImageView.Dispose();
                FriendImageView = null;
            }

            if (FriendNameLabel != null)
            {
                FriendNameLabel.RemoveFromSuperview();
                FriendNameLabel.Dispose();
                FriendNameLabel = null;
            }
        }

        private void ManageMessages()
        {
            var conn = new SQLiteConnection(ChatterDatabase.DatabasePath());
            var query = conn.Query<ChatUserModel>("select * from ChatUserModel");

            if (messages == null)
            {
                messages = new List<Message>();
            }

            foreach (var chatMessage in query)
            {
                if (!string.IsNullOrEmpty(chatMessage.Text))
                {
                    messages.Add(new Message(chatMessage.Id, chatMessage.DisplayName, DateExtention.DateTimeToNSDate(chatMessage.ChatDate), chatMessage.Text));
                }
            }
        }

        #endregion

        #region - UICollectionView Delegate/Dtatasource Methods

        public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
        {
            var cell = base.GetCell(collectionView, indexPath) as MessagesCollectionViewCell;

            var message = messages[indexPath.Row];

            if (message.SenderId.Equals(SenderId))
                cell.TextView.TextColor = UIColor.White;
            else
                cell.TextView.TextColor = UIColor.Black;

            return cell;
        }

        public override nint GetItemsCount(UICollectionView collectionView, nint section)
        {
            return messages.Count;
        }

        public override IMessageData GetMessageData(MessagesCollectionView collectionView, NSIndexPath indexPath)
        {
            return messages[indexPath.Row];
        }

        public override IMessageBubbleImageDataSource GetMessageBubbleImageData(MessagesCollectionView collectionView, NSIndexPath indexPath)
        {
            var message = messages[indexPath.Row];

            if (message.SenderId.Equals(SenderId))
                return outgoingBubbleImageData;

            return incomingBubbleImageData;
        }

        public override IMessageAvatarImageDataSource GetAvatarImageData(MessagesCollectionView collectionView, NSIndexPath indexPath)
        {
            if (messages[indexPath.Row].SenderId.Equals(CHATTER_ID))
            {
                return MessagesAvatarImageFactory.CreateAvatarImage(UIImage.FromBundle("friend"), 25);
            };
            return null;
        }

        public override NSAttributedString GetMessageBubbleTopLabelAttributedText(MessagesCollectionView collectionView, NSIndexPath indexPath)
        {
            switch (messages[indexPath.Row].SenderId)
            {
                //case JAYENDRA_ID:
                //    return new NSAttributedString(JAYENDRA);

                //case ROHIT_ID:
                //return new NSAttributedString(ROHIT);

                default:
                    return null;
            }
        }

        //public override nfloat GetMessageBubbleTopLabelHeight(MessagesCollectionView collectionView, MessagesCollectionViewFlowLayout collectionViewLayout, NSIndexPath indexPath)
        //{
        //    return 25.0f;
        //}

        public override NSAttributedString GetCellBottomLabelAttributedText(MessagesCollectionView collectionView, NSIndexPath indexPath)
        {
            return new NSAttributedString(DateTime.Now.ToString("MMM d h:mm"));
        }

        public override nfloat GetCellBottomLabelHeight(MessagesCollectionView collectionView, MessagesCollectionViewFlowLayout collectionViewLayout, NSIndexPath indexPath)
        {
            return 25.0f;
        }

        public override void TappedMessageBubble(MessagesCollectionView collectionView, NSIndexPath indexPath)
        {

        }

        void SelectedAction(string obj, EventArgs e)
        {
            if (obj.Equals("Photo Library"))
                PickImageFromGallery();
            else if (obj.Equals("Take a Picture"))
                CaptureImageFromCamera();
        }


        void SelectedActionForBelowiOS8(object sender, UIButtonEventArgs e)
        {
            if (e.ButtonIndex == 1)
                PickImageFromGallery();
            else if (e.ButtonIndex == 2)
                CaptureImageFromCamera();
        }

        public override void PressedAccessoryButton(UIButton sender)
        {
            base.PressedAccessoryButton(sender);

            var actionController = new ActionController();

            if ((ObjCRuntime.Class.GetHandle("UIAlertController") != IntPtr.Zero))
            {
                var menuList = new List<string>();
                menuList.Add("Photo Library");
                menuList.Add("Take a Picture");
                menuList.Add("Cancel");

                UIAlertController controller = actionController.ShowActionSheetForiOS8(menuList, "Attach from");
                actionController.SelectedAction += (sender1, e) => SelectedAction((string)sender1, e);
                PresentViewController(controller, true, null);
            }
            else
            {
                var actionSheet = actionController.ShowActionSheetForBelowiOS8(new string[] {
                     "Photo Library",
                    "Take a Picture"
                });
                actionController.SelectedAction += (sender1, e) => SelectedActionForBelowiOS8(sender, e as UIButtonEventArgs);
                actionSheet.ShowInView(this.View);
            }
        }

        void PickImageFromGallery()
        {
            if (UIImagePickerController.IsSourceTypeAvailable(UIImagePickerControllerSourceType.PhotoLibrary))
            {
                var galleryController = new UIImagePickerController();
                galleryController.SourceType = UIImagePickerControllerSourceType.PhotoLibrary;
                galleryController.AllowsEditing = true;
                galleryController.FinishedPickingMedia += FinishedPickingImageFromMedia;
                galleryController.Canceled += CancelPickingMedia;
                isNavigatedForward = true;
                NavigationController.PresentModalViewController(galleryController, true);
            }
        }

        void CaptureImageFromCamera()
        {
            if (UIImagePickerController.IsSourceTypeAvailable(UIImagePickerControllerSourceType.Camera))
            {
                var cameraController = new UIImagePickerController();
                cameraController.SourceType = UIImagePickerControllerSourceType.Camera;
                cameraController.AllowsEditing = false;
                cameraController.Canceled += CancelPickingMedia;
                cameraController.FinishedPickingMedia += FinishedPickingImageFromMedia;
                isNavigatedForward = true;
                NavigationController.PresentModalViewController(cameraController, true);
            }
            else
            {
                new UIAlertView("Alert", "can't access device camera.", null, "Ok", null).Show();
            }
        }

        protected void CancelPickingMedia(object sender, EventArgs e)
        {
            UIImagePickerController galleryController = sender as UIImagePickerController;
            galleryController.DismissViewController(true, null);
        }

        protected void FinishedPickingImageFromMedia(object sender, UIImagePickerMediaPickedEventArgs e)
        {
            var galleryController = sender as UIImagePickerController;

            bool isImage = false;

            switch (e.Info[UIImagePickerController.MediaType].ToString())
            {
                case "public.image":
                    isImage = true;
                    break;
                case "public.video":
                    break;
            }

            if (isImage)
            {
                var image = e.Info[UIImagePickerController.OriginalImage] as UIImage;
                if (image != null)
                {
                    string imageName = null;
                    string extension = null;

                    var refUrl = e.Info[UIImagePickerController.ReferenceUrl] as NSUrl;
                    if (refUrl != null)
                    {
                        var assetsLibrary = new ALAssetsLibrary();
                        assetsLibrary.AssetForUrl(refUrl,
                            async resultBlock =>
                            {
                                var imageRepresentation = resultBlock.DefaultRepresentation;
                                imageName = imageRepresentation.Filename;
                                imageName = imageName.ToLower();
                                extension = imageName.Split('.')[1];

                                var imageData = image.AsJPEG(0.3f);

                                var photoMediaItem = new PhotoMediaItem(image);


                                if (!Reachability.IsNetworkAvailable())
                                {
                                    new UIAlertView("Alert", "Please check your internet connection!", null, "Ok", null).Show();
                                    return;
                                }

                                SystemSoundPlayer.PlayMessageSentSound();

                                if (client == null)
                                {
                                    CreateMqttConnection();
                                }

                                client.Publish(TOPIC_PUBLISH, imageData.ToArray(), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, false);

                                var message1 = new Message(SenderId, SenderDisplayName, NSDate.Now, photoMediaItem);
                                if (message1 != null)
                                {
                                    messages.Add(message1);

                                    var conn = new SQLiteConnection(ChatterDatabase.DatabasePath());
                                    var s = conn.Insert(new ChatUserModel()
                                    {
                                        Id = SenderId,
                                        DisplayName = SenderDisplayName,
                                        ChatDate = DateTime.Now,
                                        Text = imageName
                                    });
                                }

                                FinishSendingMessage(true);

                                await Task.Delay(500);


                                var message2 = new Message(friend.Id, friend.DisplayName, NSDate.Now, "Hello, Ramakant!!! your State: Maharashtra, Town: Sangali, Fruit: Mango, Current Rate: 150/Kg, Logistics: Rajeev Transport 400 per trip");
                                if (message2 != null)
                                {
                                    messages.Add(message2);

                                    var conn = new SQLiteConnection(ChatterDatabase.DatabasePath());
                                    var s = conn.Insert(new ChatUserModel()
                                    {
                                        Id = friend.Id,
                                        DisplayName = friend.DisplayName,
                                        ChatDate = DateTime.Now,
                                        Text = "Hello, Ramakant!!! your State: Maharashtra, Town: Sangali, Fruit: Mango, Current Rate: 150/Kg, Logistics: Rajeev Transport 400 per trip"
                                    });
                                }

                                FinishSendingMessage(true);

                                await Task.Delay(500);
                            },
                            failureBlock =>
                            {
                            });
                    }
                    else
                    {

                    }
                }
            }

            galleryController.DismissViewController(true, null);
        }


        #endregion

    }

    public class ChatUser
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
    }
}
