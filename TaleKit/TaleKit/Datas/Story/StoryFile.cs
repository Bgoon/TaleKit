﻿using GKit.Json;
using GKitForUnity.Data;
using GKitForUnity.IO;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TaleKit.Datas.ModelEditor;
using TaleKit.Datas.UI.UIItem;

namespace TaleKit.Datas.Story {

	//StoryData {
	//	Scenes {
	//		Clip {} []
	//	}
	//	Clips {
	//		Clip {} []
	//	}
	//}
	public class StoryFile : ITaleDataFile {
		public readonly TaleData OwnerTaleData;

		public StoryClip RootClip {
			get; private set;
		}

		public Dictionary<string, StoryClip> GUID_To_ClipDict {
			get; private set;
		}

		public readonly UiCacheManager UiCacheManager;

		public event NodeItemDelegate<StoryBlockBase, StoryClip> BlockCreated;
		public event NodeItemDelegate<StoryBlockBase, StoryClip> BlockRemoved;

		public event Action<StoryClip> ClipCreated;
		public event Action<StoryClip> ClipRemoved;

		public StoryFile(TaleData ownerTaleData) {
			// Init members
			this.OwnerTaleData = ownerTaleData;

			GUID_To_ClipDict = new Dictionary<string, StoryClip>();
			UiCacheManager = new UiCacheManager(this);

			if(!ownerTaleData.InitArgs.onDataLoad) {
				RootClip = CreateStoryClip();
				RootClip.name = "Root";
			}

			// Register events
			ownerTaleData.Tick += UiCacheManager.OnTick;
		}

		// [ Data ]
		public JObject ToJObject() {
			JObject jFile = new JObject();

			//Add clips
			JArray jClips = new JArray();
			jFile.Add("Clips", jClips);

			foreach (KeyValuePair<string, StoryClip> clipPair in GUID_To_ClipDict) {
				StoryClip clip = clipPair.Value;
				if (clip == RootClip)
					continue;

				JObject jClip = clip.ToJObject();
				jClips.Add(jClip);
			}

			//Add rootClip
			jFile.Add("RootClip", RootClip.ToJObject());

			return jFile;
		}
		public bool LoadFromJson(JObject jStoryFile) {
			
			JObject jRootClip = jStoryFile["RootClip"].ToObject<JObject>();
			RootClip = LoadClip(jRootClip);

			JArray jClips = jStoryFile["Clips"].ToObject<JArray>();
			foreach(JObject jClip in jClips) {
				LoadClip(jClip);
			}

			StoryClip LoadClip(JObject jClip) {
				JObject jFields = jClip["Fields"].ToObject<JObject>();
				string clipGuid = jFields["guid"].ToObject<string>();

				StoryClip clip = CreateStoryClip(clipGuid);
				clip.LoadAttrFields<SavableFieldAttribute>(jFields);

				JArray jBlocks = jClip["Blocks"].ToObject<JArray>();

				foreach (JObject jBlock in jBlocks) {
					LoadBlock(jBlock, clip);
				}
				return clip;
			}
			StoryBlockBase LoadBlock(JObject jBlock, StoryClip parentClip) {
				JObject jBlockFields = jBlock["Fields"].ToObject<JObject>();
				string blockGuid = jBlockFields["guid"].ToObject<string>();
				StoryBlockType blockType = jBlockFields["blockType"].ToObject<StoryBlockType>();

				StoryBlockBase block = CreateStoryBlock(parentClip, blockType, blockGuid);
				block.LoadAttrFields<SavableFieldAttribute>(jBlockFields);

				if(blockType == StoryBlockType.Item) {
					JArray jOrders = jBlock["Orders"].ToObject<JArray>();
					foreach(JObject jOrder in jOrders) {
						LoadOrder(jOrder, block as StoryBlock_Item);
					}
				}
				return block;
			}
			OrderBase LoadOrder(JObject jOrder, StoryBlock_Item ownerBlock) {
				JObject jFields = jOrder["Fields"].ToObject<JObject>();
				OrderType orderType = jFields["orderType"].ToObject<OrderType>();
				OrderBase order = ownerBlock.AddOrder(orderType);

				SerializeUtility.FieldHandlerDelegate orderLoadHandler = null;
				if (orderType == OrderType.UI) {
					Order_UI UIOrder = order as Order_UI;
					UIOrder.targetUIGuid = jFields["targetUIGuid"].ToObject<string>();

					if(UIOrder.targetUIGuid != null) {
						UIItemBase targetUI = ownerBlock.OwnerFile.OwnerTaleData.UiFile.UISnapshot.GetUiItem(UIOrder.targetUIGuid);
						UIItemBase UIKeyData = UIOrder.CreateUIKeyData();

						JObject jUIKeyData = jFields["UIKeyData"].ToObject<JObject>();
						SerializeUtility.FieldHandlerDelegate UIKeyDataLoadHandler = (object model, FieldInfo fieldInfo, ref bool skip) => {
							if(jUIKeyData.ContainsKey(fieldInfo.Name)) {
								UIKeyData.KeyFieldNameHashSet.Add(fieldInfo.Name);
							}
						};

						UIKeyData.LoadAttrFields<SavableFieldAttribute>(jUIKeyData, UIKeyDataLoadHandler);

						UIOrder.UIKeyData = UIKeyData;

						orderLoadHandler = (object model, FieldInfo fieldInfo, ref bool skip) => {
							if (fieldInfo.GetCustomAttributes<ValueEditor_ModelKeyFrameAttribute>() != null) {
								skip = true;
							}
						};
					}

				}

				order.LoadAttrFields<SavableFieldAttribute>(jFields, orderLoadHandler);

				return order;
			}

			return true;
		}

		// [ Item managing ]
		public StoryBlockBase CreateStoryBlock(StoryClip parentClip, StoryBlockType blockType, string guid = null) {
			if (parentClip == null)
				parentClip = RootClip;

			StoryBlockBase block = null;
			switch(blockType) {
				case StoryBlockType.Item:
					block = new StoryBlock_Item(this);
					break;
				case StoryBlockType.Clip:
					block = new StoryBlock_Clip(this);
					break;
			}
			if(guid != null) {
				block.guid = guid;
			}

			BlockCreated?.Invoke(block, parentClip);

			parentClip.AddChildItem(block);

			return block;
		}
		public void RemoveStoryBlock(StoryBlockBase item) {
			StoryClip clip = item.ParentClip;
			clip.BlockItemList.Remove(item);

			BlockRemoved?.Invoke(item, clip);
		}

		public StoryClip CreateStoryClip(string guid = null) {
			StoryClip clip = new StoryClip(this);
			if(guid != null) {
				clip.guid = guid;
			}

			GUID_To_ClipDict.Add(clip.guid, clip);

			ClipCreated?.Invoke(clip);

			return clip;
		}
		public void RemoveStoryClip(StoryClip clip) {
			foreach (StoryBlockBase childItem in clip.BlockItemList) {
				RemoveStoryBlock(childItem);
			}
			GUID_To_ClipDict.Remove(clip.guid);

			ClipRemoved?.Invoke(clip);
		}

		private string GetNewItemName() {
			return "New object";
		}

	}
}
