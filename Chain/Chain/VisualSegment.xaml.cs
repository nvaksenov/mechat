﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Chain
{
	/// <summary>
	/// Логика взаимодействия для VisualJoint.xaml
	/// </summary>
	public partial class VisualSegment : VisualObject
	{
		private Point _begin;
		private Point _end;

		public VisualSegment(Segment parent)
		{
			ParentObject = parent;
			InitializeComponent();
		}

		public Point Begin
		{
			get => _begin;
			set
			{
				_begin = value;
				SetPosition();
			}
		}

		public Point End
		{
			get => _end;
			set
			{
				_end = value;
				SetPosition();
			}
		}

		public double GraphicalLength => (End - Begin).Length;

		private void SetPosition()
		{
			var halfHeight = Begin.X - ActualHeight / 2;

			var sinAngle = (End.Y - Begin.Y) / GraphicalLength;
			var cosAngle = (End.X - Begin.X) / GraphicalLength;

			double angle = 0;
			if (End.X - Begin.X >= 0)
				angle = Math.Asin(sinAngle) * 180 / Math.PI;

			if (End.X - Begin.X < 0 && End.Y - Begin.Y >= 0)
				angle = 90 + Math.Asin(-cosAngle) * 180 / Math.PI;

			if (End.X - Begin.X < 0 && End.Y - Begin.Y < 0)
				angle = -90 + Math.Asin(sinAngle) * 180 / Math.PI;

			Margin = new Thickness(Begin.Y, halfHeight, Begin.Y, halfHeight);
			RenderTransform = new RotateTransform(angle, 0, ActualHeight / 2);

			NotifyPropertyChanged(() => GraphicalLength);
		}

		public override event Action<VisualObject> OnSelectedChanged;

		private void OnSelected(object sender, MouseButtonEventArgs e)
		{
			OnSelectedChanged?.Invoke(this);
		}
	}
}