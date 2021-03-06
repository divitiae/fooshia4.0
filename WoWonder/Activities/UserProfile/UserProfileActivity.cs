using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AFollestad.MaterialDialogs;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Ads;
using Android.Graphics;
using Android.OS; 
using Android.Text;
using Android.Views;
using Android.Widget;
using AndroidX.Core.Content;
using AndroidX.SwipeRefreshLayout.Widget;
using AT.Markushi.UI;
using Bumptech.Glide;
using Bumptech.Glide.Request;
using Com.Tuyenmonkey.Textdecorator;
using Google.Android.Material.AppBar;
using Java.Lang;
using Newtonsoft.Json;
using WoWonder.Library.Anjo.Share;
using WoWonder.Library.Anjo.Share.Abstractions;
using Refractored.Controls;
using WoWonder.Activities.Base;
using WoWonder.Activities.Contacts;
using WoWonder.Activities.Gift;
using WoWonder.Activities.Live.Utils;
using WoWonder.Activities.NativePost.Extra;
using WoWonder.Activities.NativePost.Post;
using WoWonder.Activities.UsersPages;
using WoWonder.Helpers.Ads;
using WoWonder.Helpers.CacheLoaders;
using WoWonder.Helpers.Controller;
using WoWonder.Helpers.Fonts;
using WoWonder.Helpers.Model;
using WoWonder.Helpers.Utils;
using WoWonder.SQLite;
using WoWonderClient.Classes.Global;
using WoWonderClient.Classes.Message;
using WoWonderClient.Classes.Posts;
using WoWonderClient.Classes.Product;
using WoWonderClient.Classes.User;
using WoWonderClient.Requests;
using Console = System.Console;
using Exception = System.Exception;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;

namespace WoWonder.Activities.UserProfile
{
    [Activity(Icon = "@mipmap/icon", Theme = "@style/MyTheme", ConfigurationChanges = ConfigChanges.Locale | ConfigChanges.UiMode | ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class UserProfileActivity : BaseActivity, MaterialDialog.IListCallback, MaterialDialog.ISingleButtonCallback
    {
        #region Variables Basic

        private CollapsingToolbarLayout CollapsingToolbar;
        private AppBarLayout AppBarLayout;
        private WRecyclerView MainRecyclerView;
        private NativePostAdapter PostFeedAdapter;
        private SwipeRefreshLayout SwipeRefreshLayout;
        private CircleButton BtnAddUser, BtnMessage, BtnMore;
        private TextView TxtUsername, TxtFollowers, TxtFollowing, TxtLikes, TxtPoints;
        private TextView TxtCountFollowers, TxtCountLikes, TxtCountFollowing, TxtCountPoints;
        private ImageView UserProfileImage, CoverImage, IconBack;
        private CircleImageView OnlineView;
        private UserDataObject UserData;
        private LinearLayout LayoutCountFollowers, LayoutCountFollowing, LayoutCountLikes, CountPointsLayout, HeaderSection;
        private string SPrivacyBirth, SPrivacyFollow, SPrivacyFriend, SPrivacyMessage, IsFollowing;
        public static string SUserId;
        private string SUrlUser, SCanFollow, SUserName, GifLink;
        private bool IsPoked, IsFamily;
        private View ViewPoints, ViewLikes, ViewFollowers;
        private FeedCombiner Combiner;
        private AdView MAdView;

        #endregion

        #region General

        protected override void OnCreate(Bundle savedInstanceState)
        {
            try
            {
                base.OnCreate(savedInstanceState);
                SetTheme(AppSettings.SetTabDarkTheme ? Resource.Style.MyTheme_Dark_Base : Resource.Style.MyTheme_Base);
                 
                Methods.App.FullScreenApp(this);

                // Create your application here
                SetContentView(Resource.Layout.Native_UserProfile);

                SUserName = Intent?.GetStringExtra("name") ?? string.Empty; 
                SUserId = Intent?.GetStringExtra("UserId") ?? string.Empty;
                GifLink = Intent?.GetStringExtra("GifLink") ?? string.Empty;

                var userObject = Intent?.GetStringExtra("UserObject");
                UserData = string.IsNullOrEmpty(userObject) switch
                {
                    false => JsonConvert.DeserializeObject<UserDataObject>(userObject),
                    _ => UserData
                };

                //Get Value And Set Toolbar
                InitComponent();
                InitToolbar();
                SetRecyclerViewAdapters();

                switch (string.IsNullOrEmpty(SUserName))
                {
                    case false:
                        GetDataUserByName();
                        break;
                    default:
                    {
                        if (UserData != null)
                            LoadPassedDate(UserData);

                        StartApiService();
                        break;
                    }
                }
                GetGiftsLists();

                AdsGoogle.Ad_Interstitial(this);
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }

        protected override void OnResume()
        {
            try
            {
                base.OnResume();
                AddOrRemoveEvent(true);
                MAdView?.Resume();
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }

        protected override void OnPause()
        {
            try
            {
                base.OnPause();
                AddOrRemoveEvent(false);
                MAdView?.Pause();
                MainRecyclerView?.StopVideo();
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }
         
        protected override void OnStop()
        {
            try
            {
                base.OnStop();
                MainRecyclerView?.StopVideo();
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }
        public override void OnTrimMemory(TrimMemory level)
        {
            try
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                base.OnTrimMemory(level);
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }

        public override void OnLowMemory()
        {
            try
            {
                GC.Collect(GC.MaxGeneration);
                base.OnLowMemory();
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }

        protected override void OnDestroy()
        {
            try
            { 
                MainRecyclerView.ReleasePlayer();
                SUserId = "";
                MAdView?.Destroy(); 
                DestroyBasic();
                base.OnDestroy();
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }

        #endregion

        #region Menu

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    OverridePendingTransition(Resource.Animation.slide_out_left, Resource.Animation.slide_out_left);
                    Finish(); 
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        #endregion

        #region Functions

        private void InitComponent()
        {
            try
            {
                CollapsingToolbar = (CollapsingToolbarLayout)FindViewById(Resource.Id.collapsingToolbar);
                CollapsingToolbar.Title = "";

                AppBarLayout = FindViewById<AppBarLayout>(Resource.Id.appBarLayout);
                AppBarLayout?.SetExpanded(true);

                BtnAddUser = (CircleButton)FindViewById(Resource.Id.AddUserbutton);
                BtnMessage = (CircleButton)FindViewById(Resource.Id.message_button);
                BtnMore = (CircleButton)FindViewById(Resource.Id.morebutton);

                IconBack = (ImageView)FindViewById(Resource.Id.back);
                TxtUsername = (TextView)FindViewById(Resource.Id.username_profile);
                TxtCountFollowers = (TextView)FindViewById(Resource.Id.CountFollowers);
                TxtCountFollowing = (TextView)FindViewById(Resource.Id.CountFollowing);
                TxtCountLikes = (TextView)FindViewById(Resource.Id.CountLikes);
                TxtCountPoints = (TextView)FindViewById(Resource.Id.CountPoints);
                TxtFollowers = FindViewById<TextView>(Resource.Id.txtFollowers);
                TxtFollowing = FindViewById<TextView>(Resource.Id.txtFollowing);
                TxtLikes = FindViewById<TextView>(Resource.Id.txtLikes);
                TxtPoints = FindViewById<TextView>(Resource.Id.txtPoints);
                UserProfileImage = (ImageView)FindViewById(Resource.Id.profileimage_head);
                CoverImage = (ImageView)FindViewById(Resource.Id.cover_image);
                OnlineView = FindViewById<CircleImageView>(Resource.Id.online_view);
                MainRecyclerView = FindViewById<WRecyclerView>(Resource.Id.newsfeedRecyler);
                HeaderSection = FindViewById<LinearLayout>(Resource.Id.headerSection);
                LayoutCountFollowers = FindViewById<LinearLayout>(Resource.Id.CountFollowersLayout);
                LayoutCountFollowing = FindViewById<LinearLayout>(Resource.Id.CountFollowingLayout);
                LayoutCountLikes = FindViewById<LinearLayout>(Resource.Id.CountLikesLayout);
                CountPointsLayout = FindViewById<LinearLayout>(Resource.Id.CountPointsLayout);

                ViewPoints = FindViewById<View>(Resource.Id.ViewPoints);
                ViewLikes = FindViewById<View>(Resource.Id.ViewLikes);
                ViewFollowers = FindViewById<View>(Resource.Id.ViewFollowers);

                SwipeRefreshLayout = FindViewById<SwipeRefreshLayout>(Resource.Id.swipeRefreshLayout);
                SwipeRefreshLayout.SetColorSchemeResources(Android.Resource.Color.HoloBlueLight, Android.Resource.Color.HoloGreenLight, Android.Resource.Color.HoloOrangeLight, Android.Resource.Color.HoloRedLight);
                SwipeRefreshLayout.Refreshing = false;
                SwipeRefreshLayout.Enabled = true;
                SwipeRefreshLayout.SetProgressBackgroundColorSchemeColor(AppSettings.SetTabDarkTheme ? Color.ParseColor("#424242") : Color.ParseColor("#f7f7f7"));


                switch (AppSettings.FlowDirectionRightToLeft)
                {
                    case true:
                        IconBack.SetImageResource(Resource.Drawable.ic_action_ic_back_rtl);
                        break;
                }

                switch (AppSettings.ConnectivitySystem)
                {
                    // Following
                    case 1:
                        TxtFollowers.Text = GetText(Resource.String.Lbl_Followers);
                        TxtFollowing.Text = GetText(Resource.String.Lbl_Following);
                        break;
                    // Friend
                    default:
                        TxtFollowers.Text = GetText(Resource.String.Lbl_Friends);
                        TxtFollowing.Text = GetText(Resource.String.Lbl_Post);
                        break;
                }

                BtnAddUser.Visibility = ViewStates.Invisible;
                BtnMore.Visibility = ViewStates.Visible;
                BtnMessage.Visibility = ViewStates.Invisible;

                switch (AppSettings.ShowUserPoint)
                {
                    case false:
                        ViewPoints.Visibility = ViewStates.Gone;
                        CountPointsLayout.Visibility = ViewStates.Gone;

                        HeaderSection.WeightSum = 3;
                        break;
                }

                OnlineView.Visibility = ViewStates.Gone;

                MAdView = FindViewById<AdView>(Resource.Id.adView);
                AdsGoogle.InitAdView(MAdView, MainRecyclerView);
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }

        private void InitToolbar()
        {
            try
            {
                var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
                if (toolbar != null)
                {
                    toolbar.Title = " ";
                    toolbar.SetTitleTextColor(Color.Black);
                    SetSupportActionBar(toolbar);
                    SupportActionBar.SetDisplayShowCustomEnabled(true);
                    SupportActionBar.SetDisplayHomeAsUpEnabled(true);
                    SupportActionBar.SetHomeButtonEnabled(true);
                    SupportActionBar.SetDisplayShowHomeEnabled(true);
                }
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }

        private void SetRecyclerViewAdapters()
        {
            try
            {
                PostFeedAdapter = new NativePostAdapter(this, SUserId, MainRecyclerView, NativeFeedType.User);
                MainRecyclerView.SetXAdapter(PostFeedAdapter, SwipeRefreshLayout);
                Combiner = new FeedCombiner(null, PostFeedAdapter.ListDiffer, this); 
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }

        private void AddOrRemoveEvent(bool addEvent)
        {
            try
            {
                switch (addEvent)
                {
                    // true +=  // false -=
                    case true:
                        SwipeRefreshLayout.Refresh += SwipeRefreshLayoutOnRefresh;
                        BtnAddUser.Click += BtnAddUserOnClick;
                        BtnMessage.Click += BtnMessageOnClick;
                        BtnMore.Click += BtnMoreOnClick;
                        IconBack.Click += IconBackOnClick;
                        LayoutCountFollowers.Click += LayoutCountFollowersOnClick;
                        LayoutCountFollowing.Click += LayoutCountFollowingOnClick;
                        LayoutCountLikes.Click += LayoutCountLikesOnClick;
                        UserProfileImage.Click += UserProfileImageOnClick;
                        CoverImage.Click += CoverImageOnClick;
                        break;
                    default:
                        SwipeRefreshLayout.Refresh -= SwipeRefreshLayoutOnRefresh;
                        BtnAddUser.Click -= BtnAddUserOnClick;
                        BtnMessage.Click -= BtnMessageOnClick;
                        BtnMore.Click -= BtnMoreOnClick;
                        IconBack.Click -= IconBackOnClick;
                        LayoutCountFollowers.Click -= LayoutCountFollowersOnClick;
                        LayoutCountFollowing.Click -= LayoutCountFollowingOnClick;
                        LayoutCountLikes.Click -= LayoutCountLikesOnClick;
                        UserProfileImage.Click -= UserProfileImageOnClick;
                        CoverImage.Click -= CoverImageOnClick;
                        break;
                }
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }

        private void DestroyBasic()
        {
            try
            {
                SwipeRefreshLayout = null!;
                CollapsingToolbar = null!;
                AppBarLayout = null!;
                BtnAddUser = null!;
                BtnMessage = null!;
                BtnMore = null!;
                IconBack = null!;
                TxtUsername = null!;
                TxtCountFollowers = null!;
                TxtCountFollowing = null!;
                TxtCountLikes = null!;
                TxtCountPoints = null!;
                TxtFollowers = null!;
                TxtFollowing = null!;
                TxtLikes = null!;
                TxtPoints = null!;
                UserProfileImage = null!;
                CoverImage = null!;
                OnlineView = null!;
                PostFeedAdapter = null!;
                MainRecyclerView = null!;
                HeaderSection = null!;
                LayoutCountFollowers = null!;
                LayoutCountFollowing = null!;
                LayoutCountLikes = null!;
                CountPointsLayout = null!;
                ViewPoints = null!;
                ViewLikes = null!;
                ViewFollowers = null!;
                Combiner = null!;
                SPrivacyBirth = null!;
                SPrivacyFollow = null!;
                SPrivacyFriend = null!;
                SPrivacyMessage = null!;
                SUrlUser = null!;
                SCanFollow = null!;
                SUserName = null!;
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }

        #endregion

        #region Get Profile

        private void StartApiService()
        {
            if (!Methods.CheckConnectivity())
                Toast.MakeText(this, GetString(Resource.String.Lbl_CheckYourInternetConnection), ToastLength.Short)?.Show();
            else
                PollyController.RunRetryPolicyFunction( new List<Func<Task>> { GetProfileApi , GetAlbumUserApi });
        }

        private async Task GetProfileApi()
        {
            var (apiStatus, respond) = await RequestsAsync.Global.Get_User_Data(SUserId); 
            if (apiStatus != 200 || respond is not GetUserDataObject result || result.UserData == null)
            {
                Methods.DisplayReportResult(this, respond);
            }
            else
            { 
                LoadPassedDate(result.UserData);

                switch (result.Family.Count)
                {
                    case > 0:
                    {
                        var data = result.Family.FirstOrDefault(o => o.UserData.UserId == UserDetails.UserId);
                        IsFamily = data != null!;
                        break;
                    }
                    default:
                        IsFamily = false;
                        break;
                }

                if (result.UserData.UserId != UserDetails.UserId)
                {
                    SCanFollow = result.UserData.CanFollow;

                    BtnAddUser.Visibility = SCanFollow switch
                    {
                        "0" when result.UserData.IsFollowing == "0" => ViewStates.Gone,
                        _ => BtnAddUser.Visibility
                    };

                    SetProfilePrivacy(result.UserData);
                }
                else
                {
                    BtnAddUser.Visibility = ViewStates.Gone;
                }

                switch (result.Followers.Count)
                {
                    case > 0:
                        //var ListDataUserFollowers = new ObservableCollection<UserDataObject>(result.Followers);
                        break;
                }

                switch (result.LikedPages.Count)
                {
                    case > 0:
                        RunOnUiThread(() => { LoadPagesLayout(result.LikedPages); });
                        break;
                }

                switch (result.JoinedGroups.Count)
                {
                    case > 0 when result.UserData.Details.DetailsClass != null:
                        RunOnUiThread(() => { LoadGroupsLayout(result.JoinedGroups, Methods.FunString.FormatPriceValue(Convert.ToInt32(result.UserData.Details.DetailsClass.GroupsCount))); });
                        break;
                    case > 0:
                        RunOnUiThread(() => { LoadGroupsLayout(result.JoinedGroups, Methods.FunString.FormatPriceValue(result.JoinedGroups.Count)); });
                        break;
                }

                if (SPrivacyFriend == "0" || result.UserData?.IsFollowing == "1" && SPrivacyFriend == "1" || SPrivacyFriend == "2")
                    switch (result.Following.Count)
                    {
                        case > 0 when result.UserData.Details.DetailsClass != null:
                            RunOnUiThread(() => { LoadFriendsLayout(result.Following, Methods.FunString.FormatPriceValue(Convert.ToInt32(result.UserData.Details.DetailsClass.FollowingCount))); });
                            break;
                        case > 0:
                            RunOnUiThread(() => { LoadFriendsLayout(result.Following, Methods.FunString.FormatPriceValue(result.Following.Count)); });
                            break;
                    }

                string postPrivacy;
                if (result.UserData.PostPrivacy.Contains("everyone"))
                {
                    postPrivacy = "1";
                }
                else if (result.UserData.PostPrivacy.Contains("ifollow") && result.UserData?.IsFollowing == "1" && result.UserData?.IsFollowingMe == "1")
                {
                    postPrivacy = "1";
                }
                else if (result.UserData.PostPrivacy.Contains("me")) //Lbl_People_Follow_Me
                {
                    postPrivacy = "1";
                }
                else // Lbl_No_body
                {
                    postPrivacy = "0";
                }

                switch (postPrivacy)
                {
                    case "1":
                    {
                        //##Set the AddBox place on Main RecyclerView
                        //------------------------------------------------------------------------
                        var check7 = PostFeedAdapter.ListDiffer.FirstOrDefault(a => a.TypeView == PostModelType.SocialLinks);
                        var check = PostFeedAdapter.ListDiffer.FirstOrDefault(a => a.TypeView == PostModelType.PagesBox);
                        var check2 = PostFeedAdapter.ListDiffer.FirstOrDefault(a => a.TypeView == PostModelType.GroupsBox);
                        var check3 = PostFeedAdapter.ListDiffer.FirstOrDefault(a => a.TypeView == PostModelType.FollowersBox);
                        var check4 = PostFeedAdapter.ListDiffer.FirstOrDefault(a => a.TypeView == PostModelType.ImagesBox);
                        var check5 = PostFeedAdapter.ListDiffer.FirstOrDefault(a => a.TypeView == PostModelType.AboutBox);
                        var check6 = PostFeedAdapter.ListDiffer.FirstOrDefault(a => a.TypeView == PostModelType.InfoUserBox);
                        var check8 = PostFeedAdapter.ListDiffer.FirstOrDefault(a => a.TypeView == PostModelType.EmptyState);

                        if (check7 != null)
                        {
                            Combiner.AddPostBoxPostView("user", PostFeedAdapter.ListDiffer.IndexOf(check7) + 1);
                        }
                        else if (check != null)
                        {
                            Combiner.AddPostBoxPostView("user", PostFeedAdapter.ListDiffer.IndexOf(check) + 1);
                        }
                        else if (check2 != null)
                        {
                            Combiner.AddPostBoxPostView("user", PostFeedAdapter.ListDiffer.IndexOf(check2) + 1);
                        }
                        else if (check3 != null)
                        {
                            Combiner.AddPostBoxPostView("user", PostFeedAdapter.ListDiffer.IndexOf(check3) + 1);
                        }
                        else if (check4 != null)
                        {
                            Combiner.AddPostBoxPostView("user", PostFeedAdapter.ListDiffer.IndexOf(check4) + 1);
                        }
                        else if (check5 != null)
                        {
                            Combiner.AddPostBoxPostView("user", PostFeedAdapter.ListDiffer.IndexOf(check5) + 1);
                        }
                        else if (check6 != null)
                        {
                            Combiner.AddPostBoxPostView("user", PostFeedAdapter.ListDiffer.IndexOf(check6) + 1);
                        } 
                        else if (check8 != null)
                        {
                            Combiner.AddPostBoxPostView("user", PostFeedAdapter.ListDiffer.IndexOf(check8) + 1);
                        }

                        break;
                    }
                }
                   
                switch (AppSettings.ShowSearchForPosts)
                {
                    case true:
                        Combiner.SearchForPostsView("user");
                        break;
                }

                RunOnUiThread(() =>
                {
                    try
                    {
                        PostFeedAdapter.SetLoading();
                        PostFeedAdapter.NotifyDataSetChanged();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e); 
                    } 
                });
                //------------------------------------------------------------------------ 
                 
                PollyController.RunRetryPolicyFunction(new List<Func<Task>> {() => MainRecyclerView.ApiPostAsync.FetchNewsFeedApiPosts() });
            }
        }

        private async Task GetAlbumUserApi()
        {
            var (apiStatus, respond) = await RequestsAsync.Album.GetPostByType(SUserId , "photos");
            if (apiStatus != 200 || respond is not PostObject result)
            {
                Methods.DisplayReportResult(this, respond);
            }
            else
            {
                switch (result.Data.Count)
                {
                    case > 0:
                    {
                        result.Data.RemoveAll(w => string.IsNullOrEmpty(w.PostFileFull));
                     
                        var count = result.Data.Count;
                        switch (count)
                        {
                            case > 10:
                                RunOnUiThread(() => { LoadImagesLayout(result.Data.Take(9).ToList(), Methods.FunString.FormatPriceValue(Convert.ToInt32(count.ToString()))); });
                                break;
                            case > 5:
                                RunOnUiThread(() => { LoadImagesLayout(result.Data.Take(6).ToList(), Methods.FunString.FormatPriceValue(Convert.ToInt32(count.ToString()))); });
                                break;
                            case > 0:
                                RunOnUiThread(() => { LoadImagesLayout(result.Data.ToList(), Methods.FunString.FormatPriceValue(Convert.ToInt32(count.ToString()))); });
                                break;
                        }

                        break;
                    }
                }
            }
        }

        private void LoadPassedDate(UserDataObject result)
        {
            try
            {
                UserData = result;

                GlideImageLoader.LoadImage(this, result.Avatar, UserProfileImage, ImageStyle.CircleCrop, ImagePlaceholders.Drawable);
                //GlideImageLoader.LoadImage(this, result.Cover, CoverImage, ImageStyle.FitCenter, ImagePlaceholders.Color, false);
                Glide.With(this).Load(result.Cover).Apply(new RequestOptions().Placeholder(Resource.Drawable.Cover_image).Error(Resource.Drawable.Cover_image)).Into(CoverImage);
                 
                var checkAboutBox = PostFeedAdapter.ListDiffer.FirstOrDefault(a => a.TypeView == PostModelType.AboutBox);
                switch (checkAboutBox)
                {
                    case null:
                        Combiner.AboutBoxPostView(WoWonderTools.GetAboutFinal(result), 0);
                        break;
                    default:
                        checkAboutBox.AboutModel.Description = WoWonderTools.GetAboutFinal(result);
                        //PostFeedAdapter.NotifyItemChanged(0);
                        break;
                }

                var checkInfoUserBox = PostFeedAdapter.ListDiffer.FirstOrDefault(a => a.TypeView == PostModelType.InfoUserBox);
                switch (checkInfoUserBox)
                {
                    case null:
                        Combiner.InfoUserBoxPostView(result, 2);
                        break;
                    default:
                        checkInfoUserBox.InfoUserModel.UserData = result;
                        //PostFeedAdapter.NotifyItemChanged(0);
                        break;
                }
                 
                var checkSocialBox = PostFeedAdapter.ListDiffer.FirstOrDefault(a => a.TypeView == PostModelType.SocialLinks);
                switch (checkSocialBox)
                {
                    case null:
                    {
                        var socialLinksModel = new SocialLinksModelClass
                        {
                            Facebook = result.Facebook,
                            Instegram = result.Instagram,
                            Twitter = result.Twitter,
                            Google = result.Google,
                            Vk = result.Vk,
                            Youtube = result.Youtube,
                        };

                        Combiner.SocialLinksBoxPostView(socialLinksModel, -1);
                        //PostFeedAdapter.NotifyItemInserted(0);
                        break;
                    }
                    default:
                        checkSocialBox.SocialLinksModel.Facebook = result.Facebook;
                        checkSocialBox.SocialLinksModel.Instegram = result.Instagram;
                        checkSocialBox.SocialLinksModel.Twitter = result.Twitter;
                        checkSocialBox.SocialLinksModel.Google = result.Google;
                        checkSocialBox.SocialLinksModel.Vk = result.Vk;
                        checkSocialBox.SocialLinksModel.Youtube = result.Youtube;
                        break;
                }
                 
                //TxtUsername.Text = result.Name;
                SUrlUser = result.Url;

                var font = Typeface.CreateFromAsset(Application.Context.Resources?.Assets, "ionicons.ttf");
                TxtUsername.SetTypeface(font, TypefaceStyle.Normal);

                var textHighLighter = result.Name;
                var textIsPro = string.Empty;
                 
                switch (result.Verified)
                {
                    case "1":
                        textHighLighter += " " + IonIconsFonts.CheckmarkCircle;
                        break;
                }

                switch (result.IsPro)
                {
                    case "1":
                        textIsPro = " " + IonIconsFonts.Flash;
                        textHighLighter += textIsPro;
                        break;
                }

                var decorator = TextDecorator.Decorate(TxtUsername, textHighLighter);

                switch (result.Verified)
                {
                    case "1":
                        decorator.SetTextColor(Resource.Color.Post_IsVerified, IonIconsFonts.CheckmarkCircle);
                        break;
                }
                 
                decorator.SetTextColor(Resource.Color.gnt_white, textIsPro);

                decorator.Build();

                var online = WoWonderTools.GetStatusOnline(Convert.ToInt32(result.LastseenUnixTime), result.LastseenStatus);
                OnlineView.Visibility = online switch
                {
                    true => ViewStates.Visible,
                    _ => OnlineView.Visibility
                };

                if (result.UserId == UserDetails.UserId)
                    BtnAddUser.Visibility = ViewStates.Gone;
                 
                //Set Privacy User
                //==================================
                SPrivacyBirth = result.BirthPrivacy;
                SPrivacyFollow = result.FollowPrivacy;
                SPrivacyFriend = result.FriendPrivacy;
                SPrivacyMessage = result.MessagePrivacy;
                TxtCountLikes.Text = Methods.FunString.FormatPriceValue(Convert.ToInt32(result.Details.DetailsClass.LikesCount));

                SetProfilePrivacy(result);

                TxtCountPoints.Text = AppSettings.ShowUserPoint switch
                {
                    true => Methods.FunString.FormatPriceValue(Convert.ToInt32(result.Points)),
                    _ => TxtCountPoints.Text
                };

                switch (AppSettings.ConnectivitySystem)
                {
                    // Following
                    case 1:
                        TxtFollowers.Text = GetText(Resource.String.Lbl_Followers);
                        TxtFollowing.Text = GetText(Resource.String.Lbl_Following);

                        TxtCountFollowers.Text = Methods.FunString.FormatPriceValue(Convert.ToInt32(result.Details.DetailsClass.FollowersCount));
                        TxtCountFollowing.Text = Methods.FunString.FormatPriceValue(Convert.ToInt32(result.Details.DetailsClass.FollowingCount));

                        LayoutCountFollowing.Tag = "Following";
                        break;
                    // Friend
                    default:
                        TxtFollowers.Text = GetText(Resource.String.Lbl_Friends);
                        TxtFollowing.Text = GetText(Resource.String.Lbl_Post);

                        TxtCountFollowers.Text = Methods.FunString.FormatPriceValue(Convert.ToInt32(result.Details.DetailsClass.FollowersCount));
                        TxtCountFollowing.Text = Methods.FunString.FormatPriceValue(Convert.ToInt32(result.Details.DetailsClass.PostCount));

                        LayoutCountFollowing.Tag = "Post";
                        break;
                }

                WoWonderTools.GetFile("", Methods.Path.FolderDiskImage, result.Cover.Split('/').Last(), result.Cover);
                WoWonderTools.GetFile("", Methods.Path.FolderDiskImage, result.Avatar.Split('/').Last(), result.Avatar);
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            } 
        }

        private void SetAddFriendCondition()
        {
            try
            {
                switch (IsFollowing)
                {
                    //>> Not Friend
                    case "0":
                        BtnAddUser.SetColor(Color.ParseColor("#6666ff"));
                        BtnAddUser.SetImageResource(Resource.Drawable.ic_add);
                        BtnAddUser.Drawable?.SetTint(Color.ParseColor("#ffffff"));
                        BtnAddUser.Tag = "Add";
                        break;
                    //>> Friend
                    case "1":
                        BtnAddUser.SetColor(Color.ParseColor("#efefef"));
                        BtnAddUser.SetImageResource(Resource.Drawable.ic_tick);
                        BtnAddUser.Drawable?.SetTint(Color.ParseColor("#444444"));
                        BtnAddUser.Tag = "friends";
                        break;
                    //>> Request
                    case "2":
                        BtnAddUser.SetColor(Color.ParseColor("#efefef"));
                        BtnAddUser.SetImageResource(Resource.Drawable.ic_requestAdd);
                        BtnAddUser.Drawable?.SetTint(Color.ParseColor("#444444"));
                        BtnAddUser.Tag = "request";
                        break;
                }
                 
                //if (BtnAddUser.Tag?.ToString() == "Add") //(is_following == "0") >> Not Friend
                //{
                //    if (UserData.ConfirmFollowers == "1")
                //    {
                //        BtnAddUser.SetColor(Color.ParseColor("#efefef"));
                //        BtnAddUser.SetImageResource(Resource.Drawable.ic_requestAdd);
                //        BtnAddUser.Drawable.SetTint(Color.ParseColor("#444444"));
                //        BtnAddUser.Tag = "request";  
                //    }
                //    else
                //    {
                //        BtnAddUser.SetColor(Color.ParseColor("#efefef"));
                //        BtnAddUser.SetImageResource(Resource.Drawable.ic_tick);
                //        BtnAddUser.Drawable.SetTint(Color.ParseColor("#444444"));
                //        BtnAddUser.Tag = "friends";
                //    } 
                //}
                //else if (BtnAddUser.Tag?.ToString() == "request") //(is_following == "2") >> Request
                //{
                //    BtnAddUser.SetColor(Color.ParseColor("#efefef"));
                //    BtnAddUser.SetImageResource(Resource.Drawable.ic_requestAdd);
                //    BtnAddUser.Drawable.SetTint(Color.ParseColor("#444444"));
                //    BtnAddUser.Tag = "Add";
                //}
                //else if (BtnAddUser.Tag?.ToString() == "Add") //(is_following == "1") >> Friend
                //{
                //    BtnAddUser.SetColor(Color.ParseColor("#6666ff"));
                //    BtnAddUser.SetImageResource(Resource.Drawable.ic_add);
                //    BtnAddUser.Drawable.SetTint(Color.ParseColor("#ffffff"));
                //    BtnAddUser.Tag = "Add";
                //}
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }

        private void SetProfilePrivacy(UserDataObject result)
        {
            try
            {
                switch (result.IsFollowing)
                {
                    case "0":
                        IsFollowing = "0";
                        BtnAddUser.Tag = "Add";
                        break;
                    case "1":
                        IsFollowing = "1";
                        BtnAddUser.Tag = "friends";
                        break;
                    case "2":
                        IsFollowing = "2";
                        BtnAddUser.Tag = "request";
                        break;
                }

                SetAddFriendCondition();

                BtnAddUser.Visibility = SPrivacyFollow switch
                {
                    // Everyone
                    "0" => ViewStates.Visible,
                    // People i Follow
                    "1" => result.IsFollowingMe switch
                    {
                        "0" when result.IsFollowing == "0" => ViewStates.Gone,
                        _ => result.IsFollowingMe == "0" ? ViewStates.Visible : ViewStates.Gone
                    },
                    _ => ViewStates.Gone
                };

                BtnMessage.Visibility = AppSettings.MessengerIntegration switch
                {
                    true => SPrivacyMessage switch
                    {
                        // Everyone
                        "0" => ViewStates.Visible,
                        // People i Follow
                        "1" => result.IsFollowingMe switch
                        {
                            "0" when result.IsFollowing == "0" => ViewStates.Gone,
                            _ => result.IsFollowingMe == "0" ? ViewStates.Visible : ViewStates.Gone
                        },
                        _ => ViewStates.Gone
                    },
                    _ => ViewStates.Gone
                };
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }

        private void LoadFriendsLayout(List<UserDataObject> followers, string friendsCounter)
        {
            try
            {
                if (followers?.Count > 0)
                {
                    var followersClass = new FollowersModelClass
                    {
                        TitleHead = GetText(AppSettings.ConnectivitySystem == 1 ? Resource.String.Lbl_Following : Resource.String.Lbl_Friends),
                        FollowersList = new List<UserDataObject>(followers.Take(12)),
                        More = friendsCounter
                    };

                    var check = PostFeedAdapter.ListDiffer.FirstOrDefault(a => a.TypeView == PostModelType.FollowersBox);
                    if (check != null)
                    {
                        check.FollowersModel = followersClass;
                        //PostFeedAdapter.NotifyItemInserted(PostFeedAdapter.ListDiffer.IndexOf(check) + 1);
                    }
                    else
                    {
                        Combiner.FollowersBoxPostView(followersClass, 4);
                        //PostFeedAdapter.NotifyItemInserted(1);
                    }
                }  
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }

        private void LoadImagesLayout(List<PostDataObject> images, string imagesCounter)
        {
            try
            {
                if (images?.Count > 0)
                {
                    var imagesClass = new ImagesModelClass
                    {
                        TitleHead = GetText(Resource.String.Lbl_Profile_Picture),
                        ImagesList = images,
                        More = images.Count.ToString()
                    };

                    var check = PostFeedAdapter.ListDiffer.FirstOrDefault(a => a.TypeView == PostModelType.ImagesBox);
                    if (check != null)
                    {
                        check.ImagesModel = imagesClass;
                        //PostFeedAdapter.NotifyItemInserted(PostFeedAdapter.ListDiffer.IndexOf(check) + 1);
                    }
                    else
                    {
                        Combiner.ImagesBoxPostView(imagesClass, 4);
                        //PostFeedAdapter.NotifyItemInserted(1);
                    }

                    RunOnUiThread(() => { PostFeedAdapter.NotifyDataSetChanged(); });
                    //------------------------------------------------------------------------ 
                    Console.WriteLine(imagesCounter);
                } 
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }

        private void LoadPagesLayout(List<PageClass> pages)
        {
            try
            {
                if (pages?.Count > 0)
                {
                    var checkNull = pages.Where(a => string.IsNullOrEmpty(a.PageId)).ToList();
                    switch (checkNull.Count)
                    {
                        case > 0:
                        {
                            foreach (var item in checkNull)
                                pages.Remove(item);
                            break;
                        }
                    }

                    var pagesClass = new PagesModelClass { PagesList = new List<PageClass>(pages) };

                    var check = PostFeedAdapter.ListDiffer.FirstOrDefault(a => a.TypeView == PostModelType.PagesBox);
                    if (check != null)
                    {
                        check.PagesModel = pagesClass;
                        //PostFeedAdapter.NotifyItemInserted(PostFeedAdapter.ListDiffer.IndexOf(check) + 1);
                    }
                    else
                    {
                        Combiner.PagesBoxPostView(pagesClass, 4);
                        //PostFeedAdapter.NotifyItemInserted(1);
                    }
                } 
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }

        private void LoadGroupsLayout(List<GroupClass> groups, string groupsCounter)
        {
            try
            {
                if (groups?.Count > 0)
                {
                    var groupsClass = new GroupsModelClass
                    {
                        TitleHead = GetText(Resource.String.Lbl_Groups),
                        GroupsList = new List<GroupClass>(groups.Take(12)),
                        More = groupsCounter,
                        UserProfileId = SUserId
                    };

                    var check = PostFeedAdapter.ListDiffer.FirstOrDefault(a => a.TypeView == PostModelType.GroupsBox);
                    if (check != null)
                    {
                        check.GroupsModel = groupsClass;
                        //PostFeedAdapter.NotifyItemInserted(PostFeedAdapter.ListDiffer.IndexOf(check) + 1);
                    }
                    else
                    {
                        Combiner.GroupsBoxPostView(groupsClass, 4);
                        //PostFeedAdapter.NotifyItemInserted(1);
                    } 
                } 
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }

        #endregion

        #region Event

        //Refresh
        private void SwipeRefreshLayoutOnRefresh(object sender, EventArgs e)
        {
            try
            {
                PostFeedAdapter.ListDiffer.Clear();
                PostFeedAdapter.NotifyDataSetChanged();

                StartApiService();
            }
            catch (Exception exception)
            {
                Methods.DisplayReportResultTrack(exception);
            }
        }

        //Open image Cover
        private void CoverImageOnClick(object sender, EventArgs e)
        {
            try
            {
                var media = WoWonderTools.GetFile("", Methods.Path.FolderDiskImage, UserData.Cover.Split('/').Last(), UserData.Cover);
                if (media.Contains("http"))
                {
                    Intent intent = new Intent(Intent.ActionView, Android.Net.Uri.Parse(media));
                    StartActivity(intent);
                }
                else
                {
                    Java.IO.File file2 = new Java.IO.File(media);
                    var photoUri = FileProvider.GetUriForFile(this, PackageName + ".fileprovider", file2);

                    Intent intent = new Intent(Intent.ActionPick);
                    intent.SetAction(Intent.ActionView);
                    intent.AddFlags(ActivityFlags.GrantReadUriPermission);
                    intent.SetDataAndType(photoUri, "image/*");
                    StartActivity(intent);
                }
                //var intent = new Intent(this, typeof(ViewFullPostActivity));
                //intent.PutExtra("Id", UserData.CoverPostId);
                ////intent.PutExtra("DataItem", JsonConvert.SerializeObject(e.NewsFeedClass));
                //StartActivity(intent);
            }
            catch (Exception exception)
            {
                Methods.DisplayReportResultTrack(exception);
            }
        }

        //Open image Avatar
        private void UserProfileImageOnClick(object sender, EventArgs e)
        {
            try
            {
                var media = WoWonderTools.GetFile("", Methods.Path.FolderDiskImage, UserData.Avatar.Split('/').Last(), UserData.Avatar);
                if (media.Contains("http"))
                {
                    Intent intent = new Intent(Intent.ActionView, Android.Net.Uri.Parse(media));
                    StartActivity(intent);
                }
                else
                {
                    Java.IO.File file2 = new Java.IO.File(media);
                    var photoUri = FileProvider.GetUriForFile(this, PackageName + ".fileprovider", file2);

                    Intent intent = new Intent(Intent.ActionPick);
                    intent.SetAction(Intent.ActionView);
                    intent.AddFlags(ActivityFlags.GrantReadUriPermission);
                    intent.SetDataAndType(photoUri, "image/*");
                    StartActivity(intent);
                }

                //var intent = new Intent(this, typeof(ViewFullPostActivity));
                //intent.PutExtra("Id", UserData.AvatarPostId);
                ////intent.PutExtra("DataItem", JsonConvert.SerializeObject(e.NewsFeedClass));
                //StartActivity(intent);
            }
            catch (Exception exception)
            {
                Methods.DisplayReportResultTrack(exception);
            }
        }
         
        //Event Show More : Block User , Copy Link To Profile
        private void BtnMoreOnClick(object sender, EventArgs eventArgs)
        {
            try
            {
                var arrayAdapter = new List<string>();
                var dialogList = new MaterialDialog.Builder(this).Theme(AppSettings.SetTabDarkTheme ? AFollestad.MaterialDialogs.Theme.Dark : AFollestad.MaterialDialogs.Theme.Light);

                arrayAdapter.Add(GetText(Resource.String.Lbl_Block));
                arrayAdapter.Add(GetText(Resource.String.Lbl_CopeLink));
                arrayAdapter.Add(GetText(Resource.String.Lbl_Share));
                //arrayAdapter.Add(GetText(Resource.String.Lbl_ReportThisUser)); //wael add in new version

                switch (AppSettings.ShowPokes)
                {
                    case true:
                        arrayAdapter.Add(IsPoked ? GetText(Resource.String.Lbl_Poked) : GetText(Resource.String.Lbl_Poke));
                        break;
                }

                switch (IsFamily)
                {
                    case false when AppSettings.ShowAddToFamily:
                        arrayAdapter.Add(GetText(Resource.String.Lbl_AddToFamily));
                        break;
                }
                 
                switch (AppSettings.ShowGift)
                {
                    case true when ListUtils.GiftsList.Count > 0:
                        arrayAdapter.Add(GetText(Resource.String.Lbl_SentGift));
                        break;
                }

                dialogList.Title(Resource.String.Lbl_More);
                dialogList.Items(arrayAdapter);
                dialogList.NegativeText(GetText(Resource.String.Lbl_Close)).OnNegative(this);
                dialogList.AlwaysCallSingleChoiceCallback();
                dialogList.ItemsCallback(this).Build().Show();
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }

        //Event Send Messages To User
        private void BtnMessageOnClick(object sender, EventArgs eventArgs)
        {
            try
            {
                switch (AppSettings.MessengerIntegration)
                {
                    case true when AppSettings.ShowDialogAskOpenMessenger:
                    {
                        var dialog = new MaterialDialog.Builder(this).Theme(AppSettings.SetTabDarkTheme ? AFollestad.MaterialDialogs.Theme.Dark : AFollestad.MaterialDialogs.Theme.Light);

                        dialog.Title(Resource.String.Lbl_Warning);
                        dialog.Content(GetText(Resource.String.Lbl_ContentAskOPenAppMessenger));
                        dialog.PositiveText(GetText(Resource.String.Lbl_Yes)).OnPositive((materialDialog, action) =>
                        {
                            try
                            {
                                Methods.App.OpenAppByPackageName(this, AppSettings.MessengerPackageName, "OpenChat", new ChatObject { UserId = SUserId, Name = UserData.Name, Avatar = UserData.Avatar });
                            }
                            catch (Exception exception)
                            {
                                Methods.DisplayReportResultTrack(exception);
                            }
                        });
                        dialog.NegativeText(GetText(Resource.String.Lbl_No)).OnNegative(this);
                        dialog.AlwaysCallSingleChoiceCallback();
                        dialog.Build().Show();
                        break;
                    }
                    case true:
                        Methods.App.OpenAppByPackageName(this, AppSettings.MessengerPackageName, "OpenChat", new ChatObject { UserId = SUserId, Name = UserData.Name, Avatar = UserData.Avatar });
                        break;
                }
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }

        //Event Add Friends Or Follower User
        private async void BtnAddUserOnClick(object sender, EventArgs eventArgs)
        {
            try
            {
                if (!Methods.CheckConnectivity())
                {
                    Toast.MakeText(this, GetString(Resource.String.Lbl_CheckYourInternetConnection), ToastLength.Short)?.Show();
                }
                else
                {
                    switch (IsFollowing)
                    {
                        case "0": // Add Or request friends
                            if (UserData?.ConfirmFollowers == "1" || AppSettings.ConnectivitySystem == 0)
                            {
                                IsFollowing = "2";
                                BtnAddUser.Tag = "request";
                            }
                            else
                            {
                                IsFollowing = "1";
                                BtnAddUser.Tag = "friends";
                            }  
                            break; 
                        case "1": // Remove friends
                            IsFollowing = "0";
                            BtnAddUser.Tag = "Add";
                            break;
                        case "2": // Remove request friends
                            IsFollowing = "0";
                            BtnAddUser.Tag = "Add";
                            break;
                    }

                    SetAddFriendCondition();
                     
                    var (apiStatus, respond) = await RequestsAsync.Global.Follow_User(SUserId).ConfigureAwait(false); 
                    if (apiStatus != 200 || respond is not FollowUserObject result || result.FollowStatus == null)
                    {
                        Methods.DisplayReportResult(this, respond);
                    }
                }
            }
            catch (Exception exception)
            {
                Methods.DisplayReportResultTrack(exception);
            }
        }

        //Event Back
        private void IconBackOnClick(object sender, EventArgs eventArgs)
        {
            try
            {
                OverridePendingTransition(Resource.Animation.slide_out_left, Resource.Animation.slide_out_left);
                Finish();
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }

        //Event Show All Users Followers 
        private void LayoutCountFollowersOnClick(object sender, EventArgs e)
        {
            try
            {
                if (SPrivacyFriend == "0" || UserData?.IsFollowing == "1" && SPrivacyFriend == "1" || SPrivacyFriend == "2")
                {
                    var intent = new Intent(this, typeof(MyContactsActivity));
                    intent.PutExtra("ContactsType", "Followers");
                    intent.PutExtra("UserId", SUserId);
                    StartActivity(intent);
                } 
            }
            catch (Exception exception)
            {
                Methods.DisplayReportResultTrack(exception);
            }
        }

        //Event Show All Page Likes
        private void LayoutCountLikesOnClick(object sender, EventArgs e)
        {
            try
            {
                var check = PostFeedAdapter.ListDiffer.FirstOrDefault(a => a.TypeView == PostModelType.PagesBox);
                if (check != null)
                {
                    var intent = new Intent(this, typeof(AllViewerActivity));
                    intent.PutExtra("Type", "PagesModel"); //StoryModel , FollowersModel , GroupsModel , PagesModel
                    intent.PutExtra("itemObject", JsonConvert.SerializeObject(check));
                    StartActivity(intent);
                }
            }
            catch (Exception exception)
            {
                Methods.DisplayReportResultTrack(exception);
            }
        }

        //Event Show All Users Following  
        private void LayoutCountFollowingOnClick(object sender, EventArgs e)
        {
            try
            {
                if (SPrivacyFriend == "0" || UserData?.IsFollowing == "1" && SPrivacyFriend == "1" || SPrivacyFriend == "2")
                {
                    switch (LayoutCountFollowing?.Tag?.ToString())
                    {
                        case "Following":
                        {
                            var intent = new Intent(this, typeof(MyContactsActivity));
                            intent.PutExtra("ContactsType", "Following");
                            intent.PutExtra("UserId", SUserId);
                            StartActivity(intent);
                            break;
                        }
                    }
                } 
            }
            catch (Exception exception)
            {
                Methods.DisplayReportResultTrack(exception);
            }
        }

        #endregion Event

        #region MaterialDialog

        public void OnSelection(MaterialDialog p0, View p1, int itemId, ICharSequence itemString)
        {
            try
            {
                string text = itemString.ToString();
                if (text == GetText(Resource.String.Lbl_Block))
                {
                    BlockUserButtonClick();
                }
                else if (text == GetText(Resource.String.Lbl_CopeLink))
                {
                    OnCopeLinkToProfile_Button_Click();
                }
                else if (text == GetText(Resource.String.Lbl_Share))
                {
                    OnShare_Button_Click();
                }
                else if (text == GetText(Resource.String.Lbl_ReportThisUser))
                {
                    OnReport_Button_Click();
                }
                else if (text == GetText(Resource.String.Lbl_Poke))
                {
                    if (Methods.CheckConnectivity())
                    {
                        IsPoked = true;
                        Toast.MakeText(this, GetString(Resource.String.Lbl_YouHavePoked), ToastLength.Short)?.Show();

                        PollyController.RunRetryPolicyFunction(new List<Func<Task>> { () => RequestsAsync.Global.CreatePoke(SUserId) });
                    }
                    else
                        Toast.MakeText(this, GetString(Resource.String.Lbl_CheckYourInternetConnection), ToastLength.Short)?.Show();
                }
                else if (text == GetText(Resource.String.Lbl_Poked))
                {
                    Toast.MakeText(this, GetString(Resource.String.Lbl_YouSentPoked), ToastLength.Short)?.Show();
                }
                else if (text == GetText(Resource.String.Lbl_AddToFamily))
                {
                    switch (ListUtils.FamilyList.Count)
                    {
                        case > 0:
                        {
                            var dialogList = new MaterialDialog.Builder(this).Theme(AppSettings.SetTabDarkTheme ? AFollestad.MaterialDialogs.Theme.Dark : AFollestad.MaterialDialogs.Theme.Light);

                            var arrayAdapter = ListUtils.FamilyList.Select(item => item.FamilyName.Replace("_", " ")).ToList();

                            dialogList.Title(GetText(Resource.String.Lbl_AddToFamily));
                            dialogList.Items(arrayAdapter);
                            dialogList.NegativeText(GetText(Resource.String.Lbl_Close)).OnNegative(this);
                            dialogList.AlwaysCallSingleChoiceCallback();
                            dialogList.ItemsCallback(this).Build().Show();
                            break;
                        }
                    }
                }
                else if (text == GetText(Resource.String.Lbl_SentGift))
                {
                    GiftButtonOnClick();
                }
                else
                {
                    var familyId = ListUtils.FamilyList.FirstOrDefault(a => a.FamilyName == itemString.ToString())?.FamilyId;
                    switch (string.IsNullOrEmpty(familyId))
                    {
                        case false:
                            IsFamily = true;
                            Toast.MakeText(this, GetText(Resource.String.Lbl_Sent_successfully), ToastLength.Short)?.Show();
                            PollyController.RunRetryPolicyFunction(new List<Func<Task>> { () => RequestsAsync.Global.AddToFamilyAsync(SUserId, familyId) });
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }

        public void OnClick(MaterialDialog p0, DialogAction p1)
        {
            try
            {
                if (p1 == DialogAction.Positive)
                {
                }
                else if (p1 == DialogAction.Negative)
                {
                    p0.Dismiss();
                }
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }

        //Event Block User
        private async void BlockUserButtonClick()
        {
            try
            {
                if (!Methods.CheckConnectivity())
                {
                    Toast.MakeText(this, GetString(Resource.String.Lbl_CheckYourInternetConnection), ToastLength.Short)?.Show();
                }
                else
                {
                    var (apiStatus, respond) = await RequestsAsync.Global.Block_User(SUserId, true); //true >> "block"
                    switch (apiStatus)
                    {
                        case 200:
                        {
                            var dbDatabase = new SqLiteDatabase();
                            dbDatabase.Insert_Or_Replace_OR_Delete_UsersContact(UserData, "Delete");
                        

                            Toast.MakeText(this, GetString(Resource.String.Lbl_Blocked_successfully), ToastLength.Short)?.Show();

                            OverridePendingTransition(Resource.Animation.slide_out_left, Resource.Animation.slide_out_left);
                            Finish();
                            break;
                        }
                        default:
                            Methods.DisplayReportResult(this, respond);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }

        //Event Menu >> Cope Link To Profile
        private void OnCopeLinkToProfile_Button_Click()
        {
            try
            {
                Methods.CopyToClipboard(this ,SUrlUser); 
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }

        //Event Menu >> Share
        private async void OnShare_Button_Click()
        {
            try
            {
                switch (CrossShare.IsSupported)
                {
                    //Share Plugin same as video
                    case false:
                        return;
                    default:
                        await CrossShare.Current.Share(new ShareMessage
                        {
                            Title = UserDetails.Username,
                            Text = "",
                            Url = SUrlUser
                        });
                        break;
                }
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }

        //Event Menu >> Report
        private void OnReport_Button_Click()
        {
            try
            {
                if (!Methods.CheckConnectivity())
                {
                    Toast.MakeText(this, GetString(Resource.String.Lbl_CheckYourInternetConnection), ToastLength.Short)?.Show();
                    return;
                }

                var dialog = new MaterialDialog.Builder(this).Theme(AppSettings.SetTabDarkTheme ? AFollestad.MaterialDialogs.Theme.Dark : AFollestad.MaterialDialogs.Theme.Light);
                dialog.Title(GetString(Resource.String.Lbl_ReportThisUser));
                dialog.Input(Resource.String.text, 0, false , (materialDialog, s) =>
                {
                    try
                    {
                        switch (s.Length)
                        {
                            case <= 0:
                                return;
                        }
                        var text = s.ToString();
                        //wael add remove with report_status
                        PollyController.RunRetryPolicyFunction(new List<Func<Task>> { () => RequestsAsync.Global.ReportUser(SUserId, text) });
                        Toast.MakeText(this, GetText(Resource.String.Lbl_YourReportPost), ToastLength.Short)?.Show();
                    }
                    catch (Exception e)
                    {
                        Methods.DisplayReportResultTrack(e); 
                    }
                });
                dialog.InputType(InputTypes.TextFlagImeMultiLine);
                dialog.PositiveText(GetText(Resource.String.Btn_Send)).OnPositive((materialDialog, action) =>
                {
                    try
                    {
                        if (action == DialogAction.Positive)
                        {

                        }
                        else if (action == DialogAction.Negative)
                        {
                            materialDialog.Dismiss();
                        }
                    }
                    catch (Exception e)
                    {
                        Methods.DisplayReportResultTrack(e);
                    }
                });
                dialog.NegativeText(GetText(Resource.String.Lbl_Cancel)).OnNegative(this);
                dialog.AlwaysCallSingleChoiceCallback();
                dialog.Build().Show();
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }

        //Sent Gift
        private void GiftButtonOnClick()
        {
            try
            {
                Bundle bundle = new Bundle();
                bundle.PutString("UserId", SUserId);

                GiftDialogFragment mGiftFragment = new GiftDialogFragment
                {
                    Arguments = bundle
                };

                mGiftFragment.Show(SupportFragmentManager, mGiftFragment.Tag);
            }
            catch (Exception exception)
            {
                Methods.DisplayReportResultTrack(exception);
            }
        }

        #endregion

        #region Permissions && Result

        //Result
        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            try
            {
                base.OnActivityResult(requestCode, resultCode, data);

                switch (requestCode)
                {
                    //add post
                    case 2500 when resultCode == Result.Ok:
                    {
                        if (!string.IsNullOrEmpty(data.GetStringExtra("itemObject")))
                        {
                            var postData = JsonConvert.DeserializeObject<PostDataObject>(data.GetStringExtra("itemObject"));
                            if (postData != null)
                            {
                                var countList = PostFeedAdapter.ItemCount;

                                var combine = new FeedCombiner(postData, PostFeedAdapter.ListDiffer, this);
                                combine.CombineDefaultPostSections("Top");

                                int countIndex = 1;
                                var model1 = PostFeedAdapter.ListDiffer.FirstOrDefault(a => a.TypeView == PostModelType.Story);
                                var model2 = PostFeedAdapter.ListDiffer.FirstOrDefault(a => a.TypeView == PostModelType.AddPostBox);
                                var model3 = PostFeedAdapter.ListDiffer.FirstOrDefault(a => a.TypeView == PostModelType.AlertBox);
                                var model4 = PostFeedAdapter.ListDiffer.FirstOrDefault(a => a.TypeView == PostModelType.SearchForPosts);

                                if (model4 != null)
                                    countIndex += PostFeedAdapter.ListDiffer.IndexOf(model4) + 1;
                                else if (model3 != null)
                                    countIndex += PostFeedAdapter.ListDiffer.IndexOf(model3) + 1;
                                else if (model2 != null)
                                    countIndex += PostFeedAdapter.ListDiffer.IndexOf(model2) + 1;
                                else if (model1 != null)
                                    countIndex += PostFeedAdapter.ListDiffer.IndexOf(model1) + 1;
                                else
                                    countIndex = 0;

                                PostFeedAdapter.NotifyItemRangeInserted(countIndex, PostFeedAdapter.ListDiffer.Count - countList);
                            }
                        }
                        else
                        {
                            PollyController.RunRetryPolicyFunction(new List<Func<Task>> { () => MainRecyclerView.ApiPostAsync.FetchNewsFeedApiPosts() });
                        }

                        break;
                    }
                    //Edit post
                    case 3950 when resultCode == Result.Ok:
                    {
                        var postId = data.GetStringExtra("PostId") ?? "";
                        var postText = data.GetStringExtra("PostText") ?? "";
                        var diff = PostFeedAdapter.ListDiffer;
                        List<AdapterModelsClass> dataGlobal = diff.Where(a => a.PostData?.Id == postId).ToList();
                        switch (dataGlobal.Count)
                        {
                            case > 0:
                            {
                                foreach (var postData in dataGlobal)
                                {
                                    postData.PostData.Orginaltext = postText;
                                    var index = diff.IndexOf(postData);
                                    switch (index)
                                    {
                                        case > -1:
                                            PostFeedAdapter.NotifyItemChanged(index);
                                            break;
                                    }
                                }

                                var checkTextSection = dataGlobal.FirstOrDefault(w => w.TypeView == PostModelType.TextSectionPostPart);
                                switch (checkTextSection)
                                {
                                    case null:
                                    {
                                        var collection = dataGlobal.FirstOrDefault()?.PostData;
                                        var item = new AdapterModelsClass
                                        {
                                            TypeView = PostModelType.TextSectionPostPart,
                                            Id = Convert.ToInt32((int)PostModelType.TextSectionPostPart + collection?.Id),
                                            PostData = collection,
                                            IsDefaultFeedPost = true
                                        };

                                        var headerPostIndex = diff.IndexOf(dataGlobal.FirstOrDefault(w => w.TypeView == PostModelType.HeaderPost));
                                        switch (headerPostIndex)
                                        {
                                            case > -1:
                                                diff.Insert(headerPostIndex + 1, item);
                                                PostFeedAdapter.NotifyItemInserted(headerPostIndex + 1);
                                                break;
                                        }

                                        break;
                                    }
                                }

                                break;
                            }
                        }

                        break;
                    }
                    //Edit post product 
                    case 3500 when resultCode == Result.Ok:
                    {
                        if (string.IsNullOrEmpty(data.GetStringExtra("itemData"))) return;
                        var item = JsonConvert.DeserializeObject<ProductDataObject>(data.GetStringExtra("itemData"));
                        if (item != null)
                        {
                            var diff = PostFeedAdapter.ListDiffer;
                            var dataGlobal = diff.Where(a => a.PostData?.Id == item.PostId).ToList();
                            switch (dataGlobal.Count)
                            {
                                case > 0:
                                {
                                    foreach (var postData in dataGlobal)
                                    {
                                        var index = diff.IndexOf(postData);
                                        switch (index)
                                        {
                                            case > -1:
                                            {
                                                var productUnion = postData.PostData.Product?.ProductClass;
                                                if (productUnion != null) productUnion.Id = item.Id;
                                                productUnion = item;
                                                Console.WriteLine(productUnion);

                                                PostFeedAdapter.NotifyItemChanged(PostFeedAdapter.ListDiffer.IndexOf(postData));
                                                break;
                                            }
                                        }
                                    }

                                    break;
                                }
                            }
                        }

                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }

        //Permissions
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            try
            {
                base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

                switch (requestCode)
                { 
                    case 235 when grantResults.Length > 0 && grantResults[0] == Permission.Granted:
                        new LiveUtil(this).OpenDialogLive();
                        break;
                    case 235:
                        Toast.MakeText(this, GetText(Resource.String.Lbl_Permission_is_denied), ToastLength.Long)?.Show();
                        break;
                }
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }

        #endregion

        private void GetGiftsLists()
        {
            try
            {
                if (!Methods.CheckConnectivity())
                {
                    Toast.MakeText(this, GetString(Resource.String.Lbl_CheckYourInternetConnection), ToastLength.Short)?.Show();
                    return;
                }

                var sqlEntity = new SqLiteDatabase(); 
                var listGifts = sqlEntity.GetGiftsList();

                ListUtils.GiftsList = ListUtils.GiftsList.Count switch
                {
                    > 0 when listGifts?.Count > 0 => listGifts,
                    _ => ListUtils.GiftsList
                };

                PollyController.RunRetryPolicyFunction(new List<Func<Task>> { ApiRequest.GetGifts });

                
                 
                switch (string.IsNullOrEmpty(GifLink))
                {
                    case false:
                        OpenGifLink(GifLink);
                        break;
                } 
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }

        private void OpenGifLink(string imageGifLink)
        {
            try
            {
                var dialog = new MaterialDialog.Builder(this).Theme(AppSettings.SetTabDarkTheme ? AFollestad.MaterialDialogs.Theme.Dark : AFollestad.MaterialDialogs.Theme.Light)
                .Title(GetText(Resource.String.Lbl_HeSentYouGift))
                .CustomView(Resource.Layout.Post_Content_Image_Layout, true)
                .NegativeText(GetText(Resource.String.Lbl_Close)).OnNegative(new WoWonderTools.MyMaterialDialog())
                .Build();
                 
                var image = dialog.CustomView.FindViewById<ImageView>(Resource.Id.Image); 
                GlideImageLoader.LoadImage(this, imageGifLink, image, ImageStyle.CenterCrop, ImagePlaceholders.Drawable);
                  
                dialog.Show();
            }
            catch (Exception e)
            {
                Console.WriteLine(e); 
            }
        }

        private async void GetDataUserByName()
        {
            try
            {
                var (apiStatus, respond) = await RequestsAsync.Global.GetUserDataByUsername(SUserName);
                switch (apiStatus)
                {
                    case 200:
                    {
                        switch (respond)
                        {
                            case GetUserDataByUsernameObject {UserData: { }} result:
                                UserData = result.UserData;

                                SUserId = UserData.UserId;

                                SetRecyclerViewAdapters();

                                LoadPassedDate(UserData);

                                StartApiService();
                                break;
                            case GetUserDataByUsernameObject result:
                                Toast.MakeText(this, GetText(Resource.String.Lbl_NotHaveThisUser), ToastLength.Short)?.Show();

                                OverridePendingTransition(Resource.Animation.slide_out_left, Resource.Animation.slide_out_left);
                                Finish();
                                break;
                        }

                        break;
                    }
                    default:
                        Methods.DisplayReportResult(this, respond);
                        break;
                }
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }
    }
}