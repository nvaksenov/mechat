﻿using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.Win32;
using System.Xml;
using System.Xml.Linq;
using System.Windows;
using System.Globalization;

namespace Chain
{
	public class ListManager
	{
		public List<Object> ChainList;
		private readonly Panel _panel;
		private string _path = "";
		private Point _centerPoint;

		public ListManager(Panel panel)
		{
			ChainList = new List<Object>();
			_panel = panel;
		}

		public void Add(Object objectForAdding = null)
		{
			Object obj;
			if (objectForAdding != null)
				obj = objectForAdding;

			else
			{
				var isNeedToCreateSegment = ChainList.Count != 0 && ChainList.Last() is Joint;
				obj = isNeedToCreateSegment ? (Object)new Segment() : new Joint();
			}

			obj.Id = ChainList.Count;
			obj.Visual.OnSelectedChanged += Select;
			obj.ObjectChanged += OnObjectChanged;
			ChainList.Add(obj);
		}

		public void Delete(int id)
		{
			foreach (var o in ChainList)
			{
				if (o.Id < id) continue;

				o.ObjectChanged -= OnObjectChanged;
				o.Visual.OnSelectedChanged -= Select;
			}

			ChainList.RemoveAll(o => o.Id >= id);
		}

		public void Load() //из файла
		{
			var fileChoose = new OpenFileDialog();
			if (fileChoose.ShowDialog() == true)
			{
				if (fileChoose.FileName.Split('.')[fileChoose.FileName.Split('.').Length - 1].ToLower() == "xml")
					_path = fileChoose.FileName;
				else
					MessageBox.Show("Выберите файл с расширением \".xml\".");
			}
			else
				return;

			if (string.IsNullOrEmpty(_path))
				return;

			var proxyChainList = new List<Object>();

			var dataXml = new XmlDocument();
			try
			{
				dataXml.Load(_path);
			}
			catch
			{
				MessageBox.Show("Не удалось загрузить файл.");
				return;
			}

			var xRoot = dataXml.DocumentElement;
			if (xRoot == null)
				return;

			var xNode = xRoot.FirstChild;

			if (xNode.ChildNodes[0].Name != "Joint")
			{
				throw new Exception("Некорректное содержимое файла.");
			}

			try
			{
				foreach (XmlNode node in xNode.ChildNodes)
				{
					if ((node.Name != "Segment") && (node.Name != "Joint"))
					{
						throw new Exception("Некорректное содержимое файла");
					}

					if (proxyChainList.Count > 0 && (proxyChainList.Last().GetType().Name == node.Name))
					{
						throw new Exception("Некорректное содержимое файла.");
					}

					var obj = node.Name == "Joint" ? new Joint() : (Object)new Segment();

					var myClassType = obj.GetType();
					var properties = myClassType.GetProperties();

					foreach (var property in properties)
					{
						if (node.Attributes == null)
							continue;

						foreach (XmlNode attribute in node.Attributes)
						{
							if (property.Name != attribute.Name)
								continue;

							var valueType = property.PropertyType.Name;
							switch (valueType)
							{
								case "Double":
									var attr1 = double.Parse(attribute.Value, CultureInfo.InvariantCulture);
									property.SetValue(obj, attr1);
									break;

								case "Boolean":
									var attr2 = bool.Parse(attribute.Value);
									property.SetValue(obj, attr2);
									break;

								default:
									throw new Exception("Некорректное содержимое файла.");
							}

							break;
						}
					}

					if (node.Name == "Joint")
					{
						if (!(obj is Joint joi))
							continue;

						if (joi.IsAngleRestricted &&
							!(joi.AngleRestrictionLeft < joi.CurrentAngle &&
							  joi.CurrentAngle < joi.AngleRestrictionRight &&
							  joi.AngleRestrictionLeft < joi.AngleRestrictionRight))
						{
							throw new Exception("Некорректное содержимое файла. Не верные параметры углов.");
						}
					}

					proxyChainList.Add(obj);
				}

				Delete(0);

				foreach (var element in proxyChainList)
				{
					Add(element);
				}
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message, "Ошибка при загрузке файла");
			}
		}

		public void Save() //в файл
		{
			var dlg = new SaveFileDialog
			{
				FileName = "SourceData",
				DefaultExt = ".text",
				Filter = "Text documents (.xml)|*.xml"
			};

			// Show save file dialog box
			var result = dlg.ShowDialog();
			if (result != true)
				return;

			var xdoc = new XDocument(); //создаём документ

			var firstElement = new XElement("Objects"); // создаем первый элемент

			try
			{
				foreach (var element in ChainList)
				{
					Object obj = element as Joint;
					var tag = "Joint";
					if (obj == null)
					{
						obj = element as Segment;
						tag = "Segment";
					}

					var xElement = new XElement(tag);

					var myClassType = obj.GetType();
					var properties = myClassType.GetProperties();

					foreach (var property in properties)
					{
						if (property.PropertyType.Name != "Boolean" && property.PropertyType.Name != "Double")
							continue;

						var attribute = new XAttribute(property.Name, property.GetValue(obj, null));
						xElement.Add(attribute);
					}

					firstElement.Add(xElement);
				}
			}
			catch
			{
				MessageBox.Show("Ошибка исходных данных."); //
			}

			// создаем корневой элемент
			var sourceData = new XElement("SourceData");

			// добавляем в корневой элемент
			sourceData.Add(firstElement);

			// добавляем корневой элемент в документ
			xdoc.Add(sourceData);

			try
			{
				// Save document
				var filename = dlg.FileName;
				using (var file = new System.IO.StreamWriter(filename, false))
				{
					file.WriteLine(xdoc);
				}
			}
			catch
			{
				MessageBox.Show("Не удалось сохранить/перезаписать файл."); //
			}
		}

		private bool IsItBig()
		{
			var j = ChainList[0].Visual as VisualJoint;
			var canvasProperties = j.GetCurrentCanvasParameters();

			var flag = 0;
			foreach (var element in ChainList)
			{
				if (element is Joint)
				{
					var vj = (VisualJoint)element.Visual;
					if ((vj.Coordinate.X > canvasProperties.X) || (vj.Coordinate.Y > canvasProperties.Y) ||
						(vj.Coordinate.X <= 0) || (vj.Coordinate.Y <= 0))
					{
						flag++;
						break;
					}
				}
				else
				{
					var vs = (VisualSegment)element.Visual;
					if ((vs.End.X > canvasProperties.X) || (vs.End.Y > canvasProperties.Y) || (vs.End.X <= 0) ||
						(vs.End.Y <= 0))
					{
						flag++;
						break;
					}
				}
			}

			return flag != 0;
		}

		private bool IsItSmall()
		{
			if (ChainList.Count <= 1)
				return false;

			var j = ChainList[0].Visual as VisualJoint;
			var canvasProperties = j.GetCurrentCanvasParameters();

			var flag = 0;
			foreach (var element in ChainList)
			{
				if (element is Joint)
				{
					var vj = (VisualJoint)element.Visual;
					if (((vj.Coordinate.X <= 0.9 * canvasProperties.X) &&
						 (vj.Coordinate.X >= 0.1 * canvasProperties.X)) &&
						((vj.Coordinate.Y <= 0.9 * canvasProperties.Y) &&
						 (vj.Coordinate.Y >= 0.1 * canvasProperties.Y)))
					{
						flag++;
					}
				}
				else
				{
					var vs = (VisualSegment)element.Visual;
					if (((vs.End.X <= 0.9 * canvasProperties.X) && (vs.End.X >= 0.1 * canvasProperties.X)) &&
						((vs.End.Y <= 0.9 * canvasProperties.Y) && (vs.End.Y >= 0.1 * canvasProperties.Y)))
					{
						flag++;
					}
				}
			}

			return flag == ChainList.Count;
		}

		private void SetInterseced()
		{
			var intercededElementsList = Calculations.GetInetersectionElements(ChainList);
			foreach (var element in ChainList)
			{
				element.Visual.Interseced = intercededElementsList.Contains(element.Id);
			}
		}

		private void OnObjectChanged(Object obj)
		{
			var cJ = ChainList[0].Visual as VisualJoint;
			_centerPoint = Calculations.MinusPoint(cJ.Coordinate, _centerPoint);
			Calculations.ChangeMas(_centerPoint);
			_centerPoint = cJ.Coordinate;
			if (obj.Id != ChainList.Count - 1)
			{
				switch (obj)
				{
					case Joint modifiedJoint:
					{
						var currentAngle = modifiedJoint.CurrentAngle;
						for (var i = modifiedJoint.Id + 1; i < ChainList.Count; i += 2)
						{
							if (!(ChainList[i] is Segment currentSegment))
								continue;

							if (!(ChainList[i - 1] is Joint prevJoint))
								throw new ArgumentNullException(nameof(prevJoint));

							var tM = new TransformationMatrix(currentAngle, ChainList, i);
							var cM = new CoordinatesMatrix(currentSegment, prevJoint);
							var visualSegment = (VisualSegment)ChainList[i].Visual;

							var coordinates = Calculations.GetCoord(tM, cM);
							if (i + 1 < ChainList.Count)
							{
								if (ChainList[i + 1].Visual is VisualJoint vj)
									vj.Coordinate = coordinates;
							}

							visualSegment.End = coordinates;
							Calculations.CoordMas[i] = coordinates;
							if (i + 1 < Calculations.CoordMas.Count)
							{
								Calculations.CoordMas[i + 1] = coordinates;
							}
						}

						break;
					}

					case Segment modifiedSegment:
					{
						Calculations.LengthMas[obj.Id / 2] = modifiedSegment.Length * Calculations.CoefficientOfScale;
						if (!(ChainList[obj.Id - 1] is Joint prevJoint))
							throw new ArgumentNullException(nameof(prevJoint));

						var visualSegment = (VisualSegment)modifiedSegment.Visual;
						var tM = new TransformationMatrix(prevJoint.CurrentAngle, ChainList, obj.Id);
						var cM = new CoordinatesMatrix(modifiedSegment, prevJoint);
						var coordinates = Calculations.GetCoord(tM, cM);
						Calculations.CoordMas[obj.Id] = Calculations.GetCoord(tM, cM);
						Calculations.CoordMas[obj.Id + 1] = Calculations.GetCoord(tM, cM);

						var dX = coordinates.X - visualSegment.End.X;
						var dY = coordinates.Y - visualSegment.End.Y;
						visualSegment.End = coordinates;
						for (var i = obj.Id + 2; i < ChainList.Count; i += 2)
						{
							if (!(ChainList[i].Visual is VisualSegment currentVisualSegment))
								continue;

							var x = currentVisualSegment.End.X + dX;
							var y = currentVisualSegment.End.Y + dY;
							var newCoordinates = new Point(x, y);
							currentVisualSegment.End = newCoordinates;
							Calculations.CoordMas[i] = newCoordinates;
							if (i + 1 < Calculations.CoordMas.Count)
							{
								Calculations.CoordMas[i + 1] = newCoordinates;
							}
						}

						break;
					}
				}
			}
			else
			{
				if (obj.Id == 0)
				{
					if (!Calculations.CoordMas.Any())
					{
						var j = (VisualJoint)ChainList[0].Visual;
						Calculations.CoordMas.Add(j.Coordinate);
					}
				}
				else
				{
					switch (obj)
					{
						case Joint _:
						{
							var newSegmentCoordinatesEnd = Calculations.CoordMas[obj.Id - 1];
							if (obj.Id < Calculations.CoordMas.Count)
								Calculations.CoordMas[obj.Id] = newSegmentCoordinatesEnd;
							else Calculations.CoordMas.Add(newSegmentCoordinatesEnd);
							break;
						}

						case Segment lastSegment:
						{
							if (!(ChainList[obj.Id - 1] is Joint prevJoint))
								throw new ArgumentNullException(nameof(prevJoint));

							var lastVisualSegment = (VisualSegment)lastSegment.Visual;

							var tM = new TransformationMatrix(prevJoint.CurrentAngle, ChainList, obj.Id);
							if (obj.Id / 2 == Calculations.LengthMas.Count)
								Calculations.LengthMas.Add(Calculations.CoefficientOfScale * lastSegment.Length);
							var cM = new CoordinatesMatrix(lastSegment, prevJoint);
							var coordinates = Calculations.GetCoord(tM, cM);
							lastVisualSegment.End = coordinates;
							if (obj.Id < Calculations.CoordMas.Count)
								Calculations.CoordMas[obj.Id] = coordinates;
							else Calculations.CoordMas.Add(coordinates);
							break;
						}
					}
				}
			}

			RescaleIfNeeded();
			ConnectObjects(obj.Id);
			SetInterseced();
		}

		private void Select(VisualObject obj)
		{
			_panel.SelectedObject = obj.ParentObject;
		}

		private void ConnectObjects(int startId)
		{
			for (var i = startId; i < ChainList.Count; i++)
			{
				switch (ChainList[i])
				{
					case Joint currentJoint:
					{
						var visualJoint = (VisualJoint)currentJoint.Visual;
						if (i == 0)
							visualJoint.PutOnCenter();
						else
						{
							var prevVisualSegment = (VisualSegment)ChainList[i - 1].Visual;
							visualJoint.Coordinate = prevVisualSegment.End;
						}

						break;
					}

					case Segment currentSegment:
					{
						var visualSegment = (VisualSegment)currentSegment.Visual;
						var prevVisualJoint = (VisualJoint)ChainList[i - 1].Visual;
						visualSegment.Begin = prevVisualJoint.Coordinate;
						break;
					}
				}
			}
		}

		private void RescaleIfNeeded()
		{
			if (IsItBig())
			{
				Calculations.MinusScale();
				for (var i = 0; i < Calculations.LengthMas.Count; i++)
				{
					Calculations.LengthMas[i] *= Calculations.CoefficientOfScale;
				}

				ChainList[0].OnObjectChanged();
			}
			//TEST

			if (IsItSmall())
			{
				if (Calculations.CoefficientOfScale >= 1)
					return;

				Calculations.PlusScale();
				for (var i = 0; i < Calculations.LengthMas.Count; i++)
				{
					Calculations.LengthMas[i] *= Calculations.CoefficientOfScale;
				}

				ChainList[0].OnObjectChanged();
			}
		}
	}
}