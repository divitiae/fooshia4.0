using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Android.App;
using Android.Content.Res;
using Android.Graphics;

using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Bumptech.Glide;
using Bumptech.Glide.Request;
using ImageViews.Rounded;
using Java.IO;
using Java.Util;
using Refractored.Controls;
using WoWonder.Helpers.CacheLoaders;
using WoWonder.Helpers.Model;
using WoWonder.Helpers.Utils;
using WoWonderClient.Classes.Story;
using Console = System.Console;
using IList = System.Collections.IList;

namespace WoWonder.Activities.Story.Adapters
{
    public class StoryAdapter : RecyclerView.Adapter, ListPreloader.IPreloadModelProvider
    {
        public event EventHandler<StoryAdapterClickEventArgs> ItemClick;
        public event EventHandler<StoryAdapterClickEventArgs> ItemLongClick;
        private string YourImageUri;

        private readonly Activity ActivityContext;

        public ObservableCollection<GetUserStoriesObject.StoryObject> StoryList = new ObservableCollection<GetUserStoriesObject.StoryObject>();

        public StoryAdapter(Activity context)
        {
            try
            {
                HasStableIds = true;
                ActivityContext = context;

                var dataOwner = StoryList.FirstOrDefault(a => a.Type == "Your");
                switch (dataOwner)
                {
                    case null:
                        StoryList.Add(new GetUserStoriesObject.StoryObject
                        {
                            Avatar = UserDetails.Avatar,
                            Type = "Your",
                            Username = context.GetText(Resource.String.Lbl_YourStory),
                            Stories = new List<GetUserStoriesObject.StoryObject.Story>
                            {
                                new GetUserStoriesObject.StoryObject.Story
                                {
                                    Thumbnail = UserDetails.Avatar,
                                }
                            }
                        });
                        break;
                }
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }

        public override int ItemCount => StoryList?.Count ?? 0;
 
        // Create new views (invoked by the layout manager)
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            try
            {
                //Setup your layout here >> Style_Story_view
                var itemView = LayoutInflater.From(parent.Context)?.Inflate(Resource.Layout.Style_HStoryView, parent, false);
                var vh = new StoryAdapterViewHolder(itemView, Click, LongClick);
                return vh;
            }
            catch (Exception exception)
            {
                Console.WriteLine("EX:ALLEN >> "+exception);
                return null!;
            }
        }

        // Replace the contents of a view (invoked by the layout manager)
        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            try
            {
                switch (viewHolder)
                {
                    case StoryAdapterViewHolder holder:
                    {
                        var item = StoryList[position];
                        if (item != null)
                        { 
                            switch (item.Stories?.Count)
                            {
                                case > 0 when item.Stories[0].Thumbnail.Contains("http"):
                                    GlideImageLoader.LoadImage(ActivityContext, item.Stories[0]?.Thumbnail, holder.RoundImage, ImageStyle.RoundedCrop, ImagePlaceholders.Drawable);
                                    //GlideImageLoader.LoadImage(ActivityContext, item.Stories[0]?.Thumbnail, holder.Image, ImageStyle.CircleCrop, ImagePlaceholders.Drawable);
                                    break;
                                case > 0:
                                    //Glide.With(ActivityContext).Load(new File(item.Stories[0].Thumbnail)).Apply(new RequestOptions().CircleCrop().Placeholder(Resource.Drawable.ImagePlacholder_circle).Error(Resource.Drawable.ImagePlacholder_circle)).Into(holder.Image);
                                    Glide.With(ActivityContext).Load(new File(item.Stories[0].Thumbnail)).Apply(new RequestOptions().CircleCrop().Placeholder(Resource.Drawable.ImagePlacholder_circle).Error(Resource.Drawable.ImagePlacholder_circle)).Into(holder.RoundImage);
                                    break;
                            }

                            switch (item.Type)
                            {
                                case "Your":
                                {
                                    holder.Circleindicator.Visibility = ViewStates.Gone;
                                    //holder.Circleindicator.Visibility = ViewStates.Invisible;
                                    YourImageUri = item.Stories[0]?.Thumbnail;
                                    GlideImageLoader.LoadImage(ActivityContext, YourImageUri, holder.RoundImage, ImageStyle.RoundedCrop, ImagePlaceholders.Drawable);

                                    RelativeLayout.LayoutParams imgViewParams = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                                    imgViewParams.SetMargins(15, 15, 15, 15);
                                    holder.Image.LayoutParameters = imgViewParams;
                                    break;
                                }
                                default:
                                    item.ProfileIndicator ??= AppSettings.MainColor;

                                    holder.Circleindicator.BackgroundTintList = ColorStateList.ValueOf(Color.ParseColor(item.ProfileIndicator)); // Default_Color 

                                    GlideImageLoader.LoadImage(ActivityContext, YourImageUri, holder.Image, ImageStyle.CircleCrop, ImagePlaceholders.Drawable);
                                    break;
                            }


                            holder.Name.Text = Methods.FunString.SubStringCutOf(WoWonderTools.GetNameFinal(item), 12);
                         
                            switch (holder.Circleindicator.HasOnClickListeners)
                            {
                                case false:
                                    holder.Circleindicator.Click += (sender, e) => Click(new StoryAdapterClickEventArgs { View = holder.MainView, Position = position });
                                    break;
                            } 
                        }

                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                Methods.DisplayReportResultTrack(exception);
            }
        }
        public override void OnViewRecycled(Java.Lang.Object holder)
        {
            try
            {
                if (ActivityContext?.IsDestroyed != false)
                        return;

                switch (holder)
                {
                    case StoryAdapterViewHolder viewHolder:
                        //Glide.With(ActivityContext).Clear(viewHolder.Image);
                        break;
                }
                base.OnViewRecycled(holder);
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
            }
        }
        public GetUserStoriesObject.StoryObject GetItem(int position)
        {
            return StoryList[position];
        }

        public override long GetItemId(int position)
        {
            try
            {
                return position;
            }
            catch (Exception exception)
            {
                Methods.DisplayReportResultTrack(exception);
                return 0;
            }
        }

        public override int GetItemViewType(int position)
        {
            try
            {
                return position;
            }
            catch (Exception exception)
            {
                Methods.DisplayReportResultTrack(exception);
                return 0;
            }
        }

        private void Click(StoryAdapterClickEventArgs args)
        {
            ItemClick?.Invoke(this, args);
        }

        private void LongClick(StoryAdapterClickEventArgs args)
        {
            ItemLongClick?.Invoke(this, args);
        }


        public IList GetPreloadItems(int p0)
        {
            try
            {
                var d = new List<string>();
                var item = StoryList[p0];
                switch (item)
                {
                    case null:
                        return d;
                    default:
                    {
                        switch (string.IsNullOrEmpty(item.Stories[0].Thumbnail))
                        {
                            case false:
                                d.Add(item.Stories[0].Thumbnail);
                                break;
                        }

                        return d;
                    }
                }
            }
            catch (Exception e)
            {
                Methods.DisplayReportResultTrack(e);
                return Collections.SingletonList(p0);
            }
        }

        public RequestBuilder GetPreloadRequestBuilder(Java.Lang.Object p0)
        {
            return GlideImageLoader.GetPreLoadRequestBuilder(ActivityContext, p0.ToString(), ImageStyle.CircleCrop);
        } 
    }

    public class StoryAdapterViewHolder : RecyclerView.ViewHolder
    {
        public StoryAdapterViewHolder(View itemView, Action<StoryAdapterClickEventArgs> clickListener,Action<StoryAdapterClickEventArgs> longClickListener) : base(itemView)
        {
            try
            {
                MainView = itemView;

                RoundImage = MainView.FindViewById<RoundedImageView>(Resource.Id.iv_round_story);
                Image = MainView.FindViewById<ImageView>(Resource.Id.userProfileImage);
                Name = MainView.FindViewById<TextView>(Resource.Id.Txt_Username);
                Circleindicator = MainView.FindViewById<CircleImageView>(Resource.Id.profile_indicator);

                //Event
                itemView.Click += (sender, e) => clickListener(new StoryAdapterClickEventArgs{View = itemView, Position = AdapterPosition});

                Console.WriteLine(longClickListener);
            }
            catch (Exception exception)
            {
                Methods.DisplayReportResultTrack(exception);
            }
        }

        #region Variables Basic

        public View MainView { get; private set; }

        public RoundedImageView RoundImage { get; set; }
        public ImageView Image { get; set; }
        public TextView Name { get; private set; }
        public CircleImageView Circleindicator { get; private set; }

        #endregion
    }

    public class StoryAdapterClickEventArgs : EventArgs
    {
        public View View { get; set; }
        public int Position { get; set; }
    }
}