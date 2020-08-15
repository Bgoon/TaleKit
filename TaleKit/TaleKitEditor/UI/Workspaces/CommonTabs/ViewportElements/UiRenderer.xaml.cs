﻿using GKitForWPF;
using GKitForWPF.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TaleKit.Datas.Resource;
using TaleKit.Datas.UI;
using TaleKit.Datas.UI.UiItem;
using TaleKitEditor.Utility;
using UnityEngine;
using UnityEngine.UIElements;
using UAnchorPreset = GKitForUnity.AnchorPreset;
using UAxisAnchor = GKitForUnity.AxisAnchor;
using Grid = System.Windows.Controls.Grid;
using Mathf = UnityEngine.Mathf;
using UColor = UnityEngine.Color;
using UGRect = GKitForUnity.GRect;
using TaleKitEditor.UI.Windows;
using TaleKitEditor.UI.Workspaces.StoryWorkspaceTabs;
using TaleKit.Datas.Story;
using System.Reflection;
using TaleKit.Datas.Asset;
using TaleKit.Datas;

namespace TaleKitEditor.UI.Workspaces.CommonTabs.ViewportElements {
	public partial class UiRenderer : UserControl {
		private static Root Root => Root.Instance;
		private static MainWindow MainWindow => Root.MainWindow;
		private static StoryBlockTab StoryBlockTab => MainWindow.StoryWorkspace.StoryBlockTab;

		public TaleData EditingTaleData => Data.OwnerFile.OwnerTaleData;
		public AssetManager AssetManager => EditingTaleData.AssetManager;
		public StoryFile EditingStoryFile => EditingTaleData.StoryFile;
		public UiFile EditingUiFile => Data.OwnerFile;
		public UiItemBase Data {
			get; private set;
		}

		private RotateTransform rotateTransform;

		[Obsolete]
		public UiRenderer() {
			InitializeComponent();
		}
		public UiRenderer(UiItemBase data) {
			InitializeComponent();

			this.Data = data;

			rotateTransform = new RotateTransform();
			this.RenderTransform = rotateTransform;
			this.RenderTransformOrigin = new Point(0.5f, 0.5f);

			List<Grid> dataContextList = new List<Grid>() {
				PanelContext,
				TextContext,
			};

			// Remove unused contexts
			if(data.itemType == UiItemType.Panel) {
				dataContextList.Remove(PanelContext);
			} else if(data.itemType == UiItemType.Text) {
				dataContextList.Remove(TextContext);
			}
			foreach(var dataContext in dataContextList) {
				dataContext.DetachParent();
			}
		}

		public void Render(bool renderChilds = false) {
			// Render data
			RenderFromData(Data);

			// Render childs
			if (renderChilds) {
				foreach (UiRenderer childRenderer in ChildItemContext.Children) {
					childRenderer.Render(renderChilds);
				}
			}
		}
		public void RenderFromData(UiItemBase data) {
			if (data.itemType == UiItemType.Panel) {
				RenderPanel(data as UiPanel);
			} else if (data.itemType == UiItemType.Text) {
				RenderText(data as UiText);
			}
			RenderBase(data);
		}

		private void RenderPanel(UiPanel data) {
			SetProperty(data, nameof(data.color), (object value) => { SolidRenderer.Background = ((UColor)value).ToColor().ToBrush(); });
			SetProperty(data, nameof(data.imageAssetKey), (object value) => {
				AssetItem imageAsset = AssetManager.GetAsset((string)value);
				if (imageAsset != null) {
					ImageRenderer.Source = new BitmapImage(new Uri(data.GetImageAsset().AssetFilename));
				} else {
					ImageRenderer.Source = null;
				}
			});
		}
		private void RenderText(UiText data) {
			SetProperty(data, nameof(data.text), (object value) => { TextRenderer.Text = (string)value; });
			SetProperty(data, nameof(data.fontFamily), (object value) => {
				string fontFamilyName = (string)value;
				FontFamily fontFamily;
				if (!string.IsNullOrEmpty(fontFamilyName)) {
					fontFamily = new FontFamily(fontFamilyName);
				} else {

					fontFamily = new FontFamily();
				}
				TextRenderer.FontFamily = fontFamily;
			});
			SetProperty(data, nameof(data.fontSize), (object value) => { TextRenderer.FontSize = Mathf.Max(1, (int)value); });
			SetProperty(data, nameof(data.fontColor), (object value) => { TextRenderer.Foreground = ((UColor)value).ToColor().ToBrush(); });
			SetProperty(data, nameof(data.textAnchor), (object value) => {
				TextAnchor textAnchor = (TextAnchor)value;
				switch (textAnchor) {
					case TextAnchor.UpperLeft:
					case TextAnchor.MiddleLeft:
					case TextAnchor.LowerLeft:
						TextRenderer.TextAlignment = System.Windows.TextAlignment.Left;
						break;
					case TextAnchor.UpperCenter:
					case TextAnchor.MiddleCenter:
					case TextAnchor.LowerCenter:
						TextRenderer.TextAlignment = System.Windows.TextAlignment.Center;
						break;
					case TextAnchor.UpperRight:
					case TextAnchor.MiddleRight:
					case TextAnchor.LowerRight:
						TextRenderer.TextAlignment = System.Windows.TextAlignment.Right;
						break;
				}

				switch (textAnchor) {
					case TextAnchor.UpperLeft:
					case TextAnchor.UpperCenter:
					case TextAnchor.UpperRight:
						TextRenderer.VerticalAlignment = VerticalAlignment.Top;
						break;
					case TextAnchor.MiddleLeft:
					case TextAnchor.MiddleCenter:
					case TextAnchor.MiddleRight:
						TextRenderer.VerticalAlignment = VerticalAlignment.Center;
						break;
					case TextAnchor.LowerLeft:
					case TextAnchor.LowerCenter:
					case TextAnchor.LowerRight:
						TextRenderer.VerticalAlignment = VerticalAlignment.Bottom;
						break;
				}
			});
		}
		private void RenderBase(UiItemBase data) {
			//AnchorPreset and Size
			SetProperty(data, nameof(data.AnchorX), (object value) => {
				UAxisAnchor axisAnchorX = (UAxisAnchor)value;
				HorizontalAlignment = axisAnchorX.ToHorizontalAlignment();

				if (axisAnchorX == UAxisAnchor.Stretch) {
					Width = double.NaN;
				} else {
					Width = data.size.x;
				}
			});
			SetProperty(data, nameof(data.AnchorY), (object value) => {
				UAxisAnchor axisAnchorY = (UAxisAnchor)value;
				VerticalAlignment = axisAnchorY.ToVerticalAlignment();

				if (axisAnchorY == UAxisAnchor.Stretch) {
					Height = double.NaN;
				} else {
					Height = data.size.y;
				}
			});

			//Margin
			SetProperty(data, nameof(data.margin), (object value) => {
				UGRect margin = (UGRect)value;
				Margin = new Thickness(margin.xMin, margin.yMax, margin.xMax, margin.yMin);
			});

			//Rotate
			SetProperty(data, nameof(data.rotation), (object value) => { rotateTransform.Angle = (float)value; });
		}

		private void SetProperty(UiItemBase data, string propertyName, Arg1Delegate<object> applyPropertyDelegate) {
			if(!data.IsKeyFrameModel || data.KeyFieldNameHashSet.Contains(propertyName)) {
				object property;

				MemberInfo member = data.GetType().GetMember(propertyName).FirstOrDefault();
				if(member is FieldInfo) {
					property = (member as FieldInfo).GetValue(data);
				} else if(member is PropertyInfo) {
					property = (member as PropertyInfo).GetValue(data);
				} else {
					throw new Exception($"Can't find property '{propertyName}'.");
				}
				applyPropertyDelegate(property);
			}
		}
	}
}
