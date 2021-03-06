﻿using System;
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
using TaleKit.Datas.ModelEditor;
using UnityEngine;
using UGRect = GKitForUnity.GRect;

namespace TaleKitEditor.UI.ModelEditor {
	/// <summary>
	/// ValueEditorElement_Vector2.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class ValueEditor_Margin : UserControl, IValueEditor {
		public UGRect Value {
			get {
				return new UGRect(ValueTextBox_Left.Value, ValueTextBox_Bottom.Value, ValueTextBox_Right.Value, ValueTextBox_Top.Value);
			} set {
				ValueTextBox_Left.Value = value.xMin;
				ValueTextBox_Right.Value = value.xMax;
				ValueTextBox_Top.Value = value.yMax;
				ValueTextBox_Bottom.Value = value.yMin;
			}
		}
		public object EditableValue {
			get {
				return Value;
			} set {
				Value = (UGRect)value;
			}
		}

		public event EditableValueChangedDelegate EditableValueChanged;

		[Obsolete]
		public ValueEditor_Margin() {
			InitializeComponent();
		}
		public ValueEditor_Margin(ValueEditor_MarginAttribute attr) {
			InitializeComponent();

			ValueEditor_NumberBox[] numberBoxes = new ValueEditor_NumberBox[] {
				ValueTextBox_Left,
				ValueTextBox_Right,
				ValueTextBox_Top,
				ValueTextBox_Bottom,
			};
			foreach(var numberBox in numberBoxes) {
				numberBox.MinValue = attr.minValue;
				numberBox.MaxValue = attr.maxValue;
				numberBox.DragAdjustFactor = attr.dragAdjustFactor;
				numberBox.EditableValueChanged += ValueTextBox_EditableValueChanged;
			}
		}

		private void ValueTextBox_EditableValueChanged(object value) {
			EditableValueChanged?.Invoke(Value);
		}
	}
}
