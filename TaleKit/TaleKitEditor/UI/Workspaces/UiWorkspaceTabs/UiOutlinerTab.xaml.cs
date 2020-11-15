﻿using GKitForWPF;
using GKitForWPF.UI.Controls;
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
using TaleKit.Datas;
using TaleKit.Datas.ModelEditor;
using TaleKit.Datas.UI;
using TaleKit.Datas.UI.UIItem;
using TaleKitEditor.UI.Dialogs;
using TaleKitEditor.UI.Windows;
using TaleKitEditor.UI.Workspaces.CommonTabs;
using TaleKitEditor.UI.Workspaces.CommonTabs.ViewportElements;
using TaleKitEditor.Workspaces;

namespace TaleKitEditor.UI.Workspaces.UiWorkspaceTabs {
	
	
	public partial class UiOutlinerTab : UserControl {
		public delegate void ItemMovedDelegate(UIItemBase item, UIItemBase newParentItem, UIItemBase oldParentItem, int index);

		private static Root Root => Root.Instance;
		private static MainWindow MainWindow => Root.MainWindow;
		private static UIFile EditingUiFile => MainWindow.EditingTaleData.UiFile;
		private static DetailTab DetailTab => MainWindow.DetailTab;
		private static ViewportTab ViewportTab => MainWindow.ViewportTab;
		private static CommonDetailPanel CommonDetailPanel => DetailTab.CommonDetailPanel;

		public UiItemView RootItemView {
			get; private set;
		}

		// SelectedItem
		public UiItemView SelectedUiItemViewSingle {
			get {
				if (UiTreeView.SelectedItemSet.Count > 0) {
					return (UiItemView)UiTreeView.SelectedItemSet.Last();
				}
				return null;
			}
		}
		public UIItemBase SelectedUiItemSingle {
			get {
				UiItemView selectedItemView = SelectedUiItemViewSingle;
				return selectedItemView == null ? null : selectedItemView.Data;
			}
		}

		public event ItemMovedDelegate ItemMoved;

		private List<UIRenderer> focusedRendererList;

		// [ Constructor ]
		public UiOutlinerTab() {
			InitializeComponent();

			if (this.IsDesignMode())
				return;

			// Init member
			focusedRendererList = new List<UIRenderer>();

			UiTreeView.AutoApplyItemMove = false;

			// Register events
			UiItemListController.CreateItemButtonClick += UiItemListController_CreateItemButtonClick;
			UiItemListController.RemoveItemButtonClick += UiItemListController_RemoveItemButtonClick;

			UiTreeView.ItemMoved += UiTreeView_ItemMoved;

			MainWindow.ProjectPreloaded += MainWindow_ProjectPreloaded;
			MainWindow.ProjectUnloaded += MainWindow_ProjectUnloaded;
			MainWindow.WorkspaceActived += MainWindow_WorkspaceActived;

			UiTreeView.SelectedItemSet.SelectionAdded += SelectedItemSet_SelectionAdded;
			UiTreeView.SelectedItemSet.SelectionRemoved += SelectedItemSet_SelectionRemoved;
		}

		private void MainWindow_WorkspaceActived(WorkspaceComponent workspace) {
			switch(workspace.type) {
				case WorkspaceType.Ui:
					

					break;
				default:
					ClearFocusBoxVisible();
					break;
			}
		}

		// [ Event ]
		private void MainWindow_ProjectPreloaded(TaleData obj) {
			EditingUiFile.ItemCreatedPreview += UiFile_ItemCreatedPreview;
			EditingUiFile.ItemRemoved += UiFile_ItemRemoved;
		}
		private void MainWindow_ProjectUnloaded(TaleData obj) {
			EditingUiFile.ItemCreatedPreview -= UiFile_ItemCreatedPreview;
			EditingUiFile.ItemRemoved -= UiFile_ItemRemoved;
		}

		private void UiItemListController_CreateItemButtonClick() {
			// Show UiItemType menu
			Dialogs.MenuItem[] menuItems = ((UIItemType[])Enum.GetValues(typeof(UIItemType))).Select(
				(UIItemType itemType) => {
					UIItemType itemTypeInstance = itemType;
					return new Dialogs.MenuItem(itemType.ToString(), () => {
						CreateAndSelectUiItem(itemType);
					});
				}).ToArray();

			MenuPanel.ShowDialog(menuItems);

			void CreateAndSelectUiItem(UIItemType itemType) {
				UIItemBase parentItem = SelectedUiItemSingle ?? EditingUiFile.UISnapshot.rootUiItem;
				UIItemBase item = EditingUiFile.CreateUiItem(parentItem, itemType);

				UiTreeView.SelectedItemSet.SetSelectedItem(item.View as ITreeItem);
			}
			
		}
		private void UiItemListController_RemoveItemButtonClick() {
			UiItemView[] selectedItems = UiTreeView.SelectedItemSet.Select(item=>(UiItemView)item).ToArray();
			foreach (UiItemView itemView in selectedItems) {
				UIItemBase data = itemView.Data;

				EditingUiFile.RemoveUiItem(data);
			}
		}

		private void UiFile_ItemCreatedPreview(UIItemBase item, UIItemBase parentItem) {
			// 생성된 UiItem의 부모관계가 정해지기 전에 먼저 UiItemView 를 생성한다.
			// (Data_ChildInserted 이벤트에서 UiItemView를 필요로 하기 때문)

			UiItemView itemView = new UiItemView(item);

			if (parentItem == null) {
				//Create root
				itemView.SetRootItem();
				RootItemView = itemView;

				UiTreeView.ChildItemCollection.Add(itemView);
				UiTreeView.ManualRootFolder = itemView;
			} else {
				itemView.ParentItem = parentItem.View as ITreeFolder;
			}
			itemView.DisplayName = item.name;
			itemView.ItemTypeName = item.itemType.ToString();

			//Register events
			item.ChildInserted += Data_ChildInserted;
			item.ChildRemoved += Data_ChildRemoved;

			void Data_ChildInserted(int index, UIItemBase childItem) {
				UiItemView childItemView = childItem.View as UiItemView;
				itemView.ChildItemCollection.Insert(index, childItemView);
			}
			void Data_ChildRemoved(UIItemBase childItem, UIItemBase currentItem) {
				UiItemView childItemView = childItem.View as UiItemView;
				itemView.ChildItemCollection.Remove(childItemView);
			}

			UITreeChanged();
		}
		private void UiFile_ItemRemoved(UIItemBase item, UIItemBase parentItem) {
			//Remove view	
			UiItemView itemView = item.View as UiItemView;
			UiTreeView.NotifyItemRemoved(itemView);
			itemView.DetachParent();

			UITreeChanged();
		}

		private void UiTreeView_ItemMoved(ITreeItem itemView, ITreeFolder oldParentView, ITreeFolder newParentView, int index) {
			//Data에 적용하기
			UIItemBase item = ((UiItemView)itemView).Data;
			UIItemBase newParentItem = ((UiItemView)newParentView).Data;
			UiItemView oldParentItemView = (oldParentView as UiItemView);

			if(oldParentItemView != null) {
				oldParentItemView.Data.RemoveChildItem(item);
			}
			newParentItem.InsertChildItem(index, item);

			ItemMoved?.Invoke(item, newParentItem, oldParentItemView.Data, index);

			UITreeChanged();
		}

		private void SelectedItemSet_SelectionAdded(ISelectable item) {
			OnSelectionChanged();
		}
		private void SelectedItemSet_SelectionRemoved(ISelectable item) {
			OnSelectionChanged();
		}

		private void OnSelectionChanged() {
			CommonDetailPanel.DetachModel();
			DetailTab.DeactiveDetailPanel();

			UpdateRendererFocusBoxVisible();

			if (UiTreeView.SelectedItemSet.Count != 1)
				return;

			UiItemView itemView = UiTreeView.SelectedItemSet.First as UiItemView;
			CommonDetailPanel.AttachModel(itemView.Data);
			DetailTab.ActiveDetailPanel(DetailPanelType.Common);

			itemView.Data.ModelUpdated += ViewportTab.UiItemDetailPanel_UiItemValueChanged;
		}

		private void UITreeChanged() {
			EditingUiFile.OwnerTaleData.StoryFile.UiCacheManager.ClearCacheAll();
		}


		private void UpdateRendererFocusBoxVisible() {
			ClearFocusBoxVisible();

			foreach (var item in UiTreeView.SelectedItemSet) {
				UiItemView itemView = UiTreeView.SelectedItemSet.First as UiItemView;
				UIRenderer renderer = EditingUiFile.Guid_To_RendererDict[itemView.Data.guid] as UIRenderer;

				renderer.SetFocusBoxVisible(true);
				focusedRendererList.Add(renderer);
			}
			// Guidline
		}
		private void ClearFocusBoxVisible() {
			foreach (UIRenderer renderer in focusedRendererList) {
				renderer.SetFocusBoxVisible(false);
			}

			focusedRendererList.Clear();
		}
	}
}
