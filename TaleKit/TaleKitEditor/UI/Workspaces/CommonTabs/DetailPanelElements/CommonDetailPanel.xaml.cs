﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using GKitForWPF;
using TaleKit.Datas.Editor;
using TaleKit.Datas.UI;
using TaleKitEditor.UI.ValueEditors;
using TaleKitEditor.UI.Windows;

namespace TaleKitEditor.UI.Workspaces.CommonTabs {
	
	public partial class CommonDetailPanel : UserControl {
		private static Root Root => Root.Instance;
		private static MainWindow MainWindow => Root.MainWindow;
		private static UiWorkspace UiWorkspace => MainWindow.UiWorkspace;
		private static DetailTab DetailTab => MainWindow.DetailTab;
		private static ViewportTab ViewportTab => MainWindow.ViewportTab;

		public IEditableModel EditingModel {
			get; private set;
		}

		public CommonDetailPanel() {
			InitializeComponent();
			if (this.IsDesignMode())
				return;

			InitMembers();
		}
		private void InitMembers() {
		}

		public void AttachModel(IEditableModel model, ModelValueChangedDelegate modelValueChangedDelegate = null) {
			DetachModel();
			EditingModel = model;

			if (model == null)
				return;

			ValueEditorUtility.CreateValueEditorViews(EditingModel, EditorViewContext, modelValueChangedDelegate);
		}
		public void DetachModel() {
			if (EditingModel == null)
				return;

			this.EditingModel = null;

			EditorViewContext.Children.Clear();
		}
	}
}