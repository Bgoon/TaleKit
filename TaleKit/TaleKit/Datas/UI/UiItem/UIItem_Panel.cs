﻿using GKitForUnity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TaleKit.Datas.Asset;
using TaleKit.Datas.ModelEditor;
using TaleKit.Datas.Resource;
using UColor = UnityEngine.Color;

namespace TaleKit.Datas.UI.UIItem {
	[Serializable]
	public class UIItem_Panel : UIItemBase {
		[ValueEditorComponent_Header("Panel Attributes")]
		[ValueEditor_ColorBox("Color")]
		public UColor color = UColor.white;

		[ValueEditor_AssetSelector("Image", AssetType.Image)]
		public string imageAsset;

		[ValueEditor_Switch("Use NinePatch")]
		public bool useNinePatch;
		public bool UseNinePatch => useNinePatch;
		[ValueEditor_Margin("NinePatch Side Aspect", 0, 1, 0.1f, visibleCondition = nameof(UseNinePatch))]
		public GRect ninePatchSideAspect;

		public UIItem_Panel(UIFile ownerFile) : base(ownerFile, UIItemType.Panel) {

		}

		public AssetItem GetImageAsset() {
			return AssetManager.GetAsset(imageAsset);
		}
	}
}
