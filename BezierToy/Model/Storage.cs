using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace BezierToy
{
    partial class Model
    {
        public void Save(string filename)
        {
            using (XmlTextWriter tw = new XmlTextWriter(filename, Encoding.UTF8))
            {
                tw.Formatting = Formatting.Indented;
                tw.Indentation = 4;
                tw.WriteStartDocument(true);
                tw.WriteDocType("plist", "-//Apple Computer//DTD PLIST 1.0//EN", "http://www.apple.com/DTDs/PropertyList-1.0.dtd", null);
                tw.WriteStartElement("plist");
                tw.WriteAttributeString(
                  "version", "1.0");
                tw.WriteStartElement("dict");

                tw.WriteStartElement("base-curve");
                tw.WriteAttributeString(
                    "color",
                    BaseCurve.Color.ToArgb().ToString("X8")
                );
                tw.WriteEndElement();

                Vector2D startPoint = BaseCurve.Points.First();
                tw.WriteStartElement("startPoint");
                tw.WriteAttributeString(
                    "x",
                    startPoint.X.ToString(CultureInfo.InvariantCulture)
                );
                tw.WriteAttributeString(
                    "y",
                    startPoint.Y.ToString(CultureInfo.InvariantCulture)
                );
                tw.WriteEndElement();


                tw.WriteElementString("key", "points");
                //tw.WriteStartElement("array");
                tw.WriteStartElement("array");
               
                foreach (Vector2D point in BaseCurve.Points)
                {

                    Vector2D temp = point - startPoint;
                    tw.WriteElementString("string", "{"
                        + temp.X.ToString(CultureInfo.InvariantCulture)
                        + ","
                        + (-temp.Y).ToString(CultureInfo.InvariantCulture)
                        + "}");
                }
                tw.WriteEndElement();


                foreach (ReducedBezierCurve curve in ReducedCurves)
                {
                    tw.WriteStartElement("reduced-curve");
                    tw.WriteAttributeString(
                        "method",
                        Reducers.Find(r => r.Factory.CanProduce(curve.Reducer))
                            .XmlName
                    );
                    tw.WriteAttributeString("degree", curve.Degree.ToString());
                    tw.WriteAttributeString(
                        "color",
                        curve.Color.ToArgb().ToString("X8")
                    );
                    curve.Reducer.WriteCustomAttributes(tw);
                    tw.WriteEndElement();
                }

                tw.WriteEndElement();
                tw.WriteEndDocument();
            }
        }
        private Vector2D PointFromString(string str) {
            //double 
            string newstr = str.Replace("{","");
            newstr = newstr.Replace("}", "");
            string[] strs = newstr.Split(',');

            return new Vector2D(double.Parse(
                           strs[0],
                            CultureInfo.InvariantCulture
                        ), - double.Parse(
                           strs[1],
                            CultureInfo.InvariantCulture
                        ));
        }
        public void Load(string filename)
        {
            // We assume model is clear.

            XmlDocument doc = new XmlDocument();
            doc.Load(filename);

            BaseCurve.Points.Clear();
            XmlNode dictNode = doc.SelectSingleNode("plist/dict");
            XmlNode baseCurveNode = dictNode.SelectSingleNode("base-curve");
            
            BaseCurve.Color = Color.FromArgb(int.Parse(
                baseCurveNode.Attributes["color"].Value,
                NumberStyles.HexNumber
            ));

            XmlNode startPointNode = dictNode.SelectSingleNode("startPoint");
            Vector2D startPoint = new Vector2D(
                    double.Parse(
                        startPointNode.Attributes["x"].Value,
                        CultureInfo.InvariantCulture
                    ),
                    double.Parse(
                        startPointNode.Attributes["y"].Value,
                        CultureInfo.InvariantCulture
                    )
                );
            
            foreach (XmlNode pointNode in dictNode.SelectNodes("array/string"))
            {
                Vector2D vec = PointFromString(pointNode.InnerText);
                BaseCurve.Points.Add(vec + startPoint);
             }

            SelectedCurve = null;
            ReducedCurves.Clear();
            foreach (XmlNode reducedCurveNode in dictNode.SelectNodes("reduced-curve"))
            {
                ReducerRecord record = Reducers.Find(
                    r => r.XmlName == reducedCurveNode.Attributes["method"]
                        .Value
                );
                Reducer reducer = record.Factory.Produce();
                reducer.ReadCustomAttributes(reducedCurveNode);
                ReducedBezierCurve curve = new ReducedBezierCurve(
                    BaseCurve, reducer);
                curve.Degree = int.Parse(
                    reducedCurveNode.Attributes["degree"].Value);
                curve.Color = Color.FromArgb(int.Parse(
                    reducedCurveNode.Attributes["color"].Value,
                    NumberStyles.HexNumber
                ));
                ReducedCurves.Add(curve);
            }

            FileName = filename;
        }

        public void Export(string filename, ImageFormat format)
        {
            Control canvas = MainWindow.Instance.Canvas;
            using (Bitmap bitmap = new Bitmap(canvas.Width, canvas.Height))
            {
                canvas.DrawToBitmap(
                    bitmap,
                    new Rectangle(0, 0, canvas.Width, canvas.Height)
                );
                bitmap.Save(filename, format);
            }
        }
    }

    partial class ConstrainedReducer
    {
        public override void WriteCustomAttributes(XmlTextWriter tw)
        {
            tw.WriteAttributeString("continuity-0",
                ContinuityClassAt0.ToString());
            tw.WriteAttributeString("continuity-1",
                ContinuityClassAt1.ToString());
        }

        public override void ReadCustomAttributes(XmlNode node)
        {
            ContinuityClassAt0 = int.Parse(
                node.Attributes["continuity-0"].Value);
            ContinuityClassAt1 = int.Parse(
                node.Attributes["continuity-1"].Value);
        }
    }
}
