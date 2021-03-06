using System;
using Android.Graphics;
using Android.Support.V7.Widget;

namespace Xamarin.Forms.Platform.Android
{
	internal class ScrollHelper
	{
		readonly RecyclerView _recyclerView;
		Action _pendingScrollAdjustment;

		public ScrollHelper(RecyclerView recyclerView)
		{
			_recyclerView = recyclerView;
		}

		public void AdjustScroll()
		{
			_pendingScrollAdjustment?.Invoke();
		}

		public void AnimateScrollToPosition(int index, ScrollToPosition scrollToPosition)
		{
			if (scrollToPosition == ScrollToPosition.MakeVisible)
			{
				// MakeVisible matches the Android default of SnapAny, so we can just use the default
				_recyclerView.SmoothScrollToPosition(index);
			}
			else
			{
				// If we want a different ScrollToPosition, we need to create a SmoothScroller which can handle it
				var smoothScroller = new PositionalSmoothScroller(_recyclerView.Context, scrollToPosition)
				{
					TargetPosition = index
				};

				// And kick off the scroll operation
				_recyclerView.GetLayoutManager().StartSmoothScroll(smoothScroller);
			}
		}

		public void JumpScrollToPosition(int index, ScrollToPosition scrollToPosition, bool uniformSize = false)
		{
			if (scrollToPosition == ScrollToPosition.MakeVisible)
			{
				// MakeVisible is the default behavior, so we don't need to do anything special
				_recyclerView.ScrollToPosition(index);
				return;
			}

			if (!(_recyclerView.GetLayoutManager() is LinearLayoutManager linearLayoutManager))
			{
				// We don't have the ScrollToPositionWithOffset method available, so we don't have a way to 
				// handle the Forms ScrollToPosition; just default back to the MakeVisible behavior
				_recyclerView.ScrollToPosition(index);
				return;
			}

			// If ScrollToPosition is Start, then we can just use an offset of 0 and we're fine
			// (Though that may change in RTL situations or if we're stacking from the end)
			if (scrollToPosition == Xamarin.Forms.ScrollToPosition.Start)
			{
				linearLayoutManager.ScrollToPositionWithOffset(index, 0);
				return;
			}

			// For handling End or Center, things get more complicated because we need to know the size of
			// the View we're targeting. 

			// If we're using ItemSizingStrategy MeasureFirstItem, we can use any item size as a guide to figure
			// out the offset

			// TODO hartez 2018/10/03 14:00:32 Handle the MeasureFirstItem case to determine the offset and call	
			// `linearLayoutManager.ScrollToPositionWithOffset(index, offset);` here
			// Use the uniformSize parameter and pass that in based on ItemsView.ItemSizingStrategy

			// If we don't already know the size of the item, things get more complicated

			// The item may not actually exist; it may have never been realized, or it may have been recycled
			// So we need to get it on screen using ScrollToPosition, then once it's on screen we can use the 
			// width/height to make adjustments for Center/End.

			// ScrollToPosition queues up the scroll operation. It doesn't do it immediately; it requests a layout
			// After that layout is finished, the view will be available for measurement and then we can adjust
			// the scroll to get it into the right place

			// Set up our pending adjustment
			if (linearLayoutManager.CanScrollVertically())
			{
				_pendingScrollAdjustment = () => AdjustVerticalScroll(index, scrollToPosition);
			}
			else
			{
				_pendingScrollAdjustment = () => AdjustHorizontalScroll(index, scrollToPosition);
			}

			// Kick off the Scroll to get the item into view; once it's in view, the pending adjustment will kick in
			_recyclerView.ScrollToPosition(index);
		}

		Rect GetViewRect(int index)
		{
			var holder = _recyclerView.FindViewHolderForAdapterPosition(index);
			var view = holder?.ItemView;

			if (view == null)
			{
				return null;
			}

			var viewRect = new Rect();
			view.GetGlobalVisibleRect(viewRect);

			return viewRect;
		}

		void AdjustVerticalScroll(int index, ScrollToPosition scrollToPosition)
		{
			_pendingScrollAdjustment = null;

			var viewRect = GetViewRect(index);

			if (viewRect == null)
			{
				return;
			}

			var offset = 0;

			var rvRect = new Rect();
			_recyclerView.GetGlobalVisibleRect(rvRect);

			if (scrollToPosition == ScrollToPosition.Center)
			{
				offset = viewRect.CenterY() - rvRect.CenterY();
			}
			else if (scrollToPosition == ScrollToPosition.End)
			{
				offset = viewRect.Bottom - rvRect.Bottom;
			}

			_recyclerView.ScrollBy(0, offset);
		}

		void AdjustHorizontalScroll(int index, ScrollToPosition scrollToPosition)
		{
			_pendingScrollAdjustment = null;

			var viewRect = GetViewRect(index);

			if (viewRect == null)
			{
				return;
			}

			var offset = 0;

			var rvRect = new Rect();
			_recyclerView.GetGlobalVisibleRect(rvRect);

			if (scrollToPosition == ScrollToPosition.Center)
			{
				offset = viewRect.CenterX() - rvRect.CenterX();
			}
			else if (scrollToPosition == ScrollToPosition.End)
			{
				offset = viewRect.Right - rvRect.Right;
			}

			_recyclerView.ScrollBy(offset, 0);
		}
	}
}