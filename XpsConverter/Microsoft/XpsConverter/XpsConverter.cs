using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Text;
using System.Windows.Media.Imaging;
using System.Xml;

namespace Microsoft.XpsConverter
{
    internal class XpsConverter
    {
        private Package _package;
        private XpsType _from;
        private XpsType _to;
        private XpsConstants _xpsConstants;
        private HashSet<string> _xmlPartContentTypes;
        private List<PackagePart> _signatureDefinitions;

        public XpsConverter(Package package, XpsType convertFrom, XpsType convertTo)
        {
            _package = package;
            _from = convertFrom;
            _to = convertTo;
            _xpsConstants = new XpsConstants(_from, _to);
            _xmlPartContentTypes = new HashSet<string>();
            _xmlPartContentTypes.Add("application/vnd.ms-package.xps-fixeddocument+xml");
            _xmlPartContentTypes.Add("application/vnd.ms-package.xps-fixeddocumentsequence+xml");
            _xmlPartContentTypes.Add("application/vnd.ms-package.xps-fixedpage+xml");
            _xmlPartContentTypes.Add("application/vnd.ms-package.xps-discard-control+xml");
            _xmlPartContentTypes.Add("application/vnd.ms-package.xps-documentstructure+xml");
            _xmlPartContentTypes.Add("application/vnd.ms-package.xps-resourcedictionary+xml");
            _xmlPartContentTypes.Add("application/vnd.ms-package.xps-signaturedefinitions+xml");
            _xmlPartContentTypes.Add("application/vnd.ms-package.xps-storyfragments+xml");
        }

        public void Process()
        {
            CollectSignatureDefinitions();
            PackageRelationshipCollection relationships = _package.GetRelationships();
            ProcessRelationsips(_package, relationships);
            PackagePartCollection parts = _package.GetParts();
            ProcessParts(parts);
            FixSignatureDefinitionsContentType();
        }

        private void ProcessRelationsips(object owner, PackageRelationshipCollection rels)
        {
            List<PackageRelationship> list = new List<PackageRelationship>();
            foreach (PackageRelationship item in rels)
            {
                list.Add(item);
            }
            if (owner is Package)
            {
                Package package = (Package)owner;
                foreach (PackageRelationship packageRelationship in list)
                {
                    package.DeleteRelationship(packageRelationship.Id);
                }
                using (List<PackageRelationship>.Enumerator enumerator2 = list.GetEnumerator())
                {
                    while (enumerator2.MoveNext())
                    {
                        PackageRelationship packageRelationship2 = enumerator2.Current;
                        package.CreateRelationship(packageRelationship2.TargetUri, packageRelationship2.TargetMode, _xpsConstants.ConvertRelationshipType(packageRelationship2.RelationshipType), packageRelationship2.Id);
                    }
                    return;
                }
            }
            if (owner is PackagePart)
            {
                PackagePart packagePart = (PackagePart)owner;
                foreach (PackageRelationship packageRelationship3 in list)
                {
                    packagePart.DeleteRelationship(packageRelationship3.Id);
                }
                using (List<PackageRelationship>.Enumerator enumerator2 = list.GetEnumerator())
                {
                    while (enumerator2.MoveNext())
                    {
                        PackageRelationship packageRelationship4 = enumerator2.Current;
                        packagePart.CreateRelationship(packageRelationship4.TargetUri, packageRelationship4.TargetMode, _xpsConstants.ConvertRelationshipType(packageRelationship4.RelationshipType), packageRelationship4.Id);
                    }
                    return;
                }
            }
            throw new ArgumentException("Owner must be a Package or a PackagePart instance.");
        }

        private void ProcessParts(PackagePartCollection parts)
        {
            foreach (PackagePart packagePart in parts)
            {
                if (!IsPackageLevelRelationshipsPart(packagePart))
                {
                    ProcessRelationsips(packagePart, packagePart.GetRelationships());
                }
                if (IsXmlPart(packagePart.ContentType) || _signatureDefinitions.Contains(packagePart))
                {
                    ProcessXmlPart(packagePart);
                }
                if (packagePart.ContentType.Equals("image/vnd.ms-photo", StringComparison.OrdinalIgnoreCase))
                {
                    if (IsJpegXRSupported())
                    {
                        ConvertHDPhoto(packagePart);
                    }
                    else
                    {
                        Console.WriteLine("HDPhoto/JpegXR image found. Skipping conversion.");
                    }
                }
            }
        }

        private void ProcessXmlPart(PackagePart part)
        {
            Encoding encoding;
            bool flag;
            GetXmlEncoding(part, out encoding, out flag);
            using (Stream stream = part.GetStream())
            {
                using (XmlReader xmlReader = XmlReader.Create(stream))
                {
                    MemoryStream memoryStream = new MemoryStream((int)stream.Length);
                    using (XmlWriter xmlWriter = XmlWriter.Create(memoryStream, new XmlWriterSettings
                    {
                        Encoding = encoding,
                        OmitXmlDeclaration = !flag
                    }))
                    {
                        while (xmlReader.Read())
                        {
                            ConvertXmlNode(xmlReader, xmlWriter, part);
                        }
                    }
                    xmlReader.Close();
                    stream.SetLength(0L);
                    byte[] array = memoryStream.ToArray();
                    stream.Write(array, 0, array.Length);
                }
            }
        }

        private void ConvertXmlNode(XmlReader reader, XmlWriter writer, PackagePart part)
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                bool isEmptyElement = reader.IsEmptyElement;
                writer.WriteStartElement(reader.Prefix, reader.LocalName, _xpsConstants.ConvertNamespace(reader.NamespaceURI));
                string localName = reader.LocalName;
                while (reader.MoveToNextAttribute())
                {
                    string localName2 = reader.LocalName;
                    string text = reader.Value;
                    if (localName2 == "xmlns" || reader.Prefix == "xmlns")
                    {
                        text = _xpsConstants.ConvertNamespace(text);
                    }
                    if (_to == XpsType.OpenXPS && ((localName == "DocumentReference" && localName2 == "Source") || (localName == "PageContent" && localName2 == "Source") || (localName == "ImageBrush" && localName2 == "ImageSource") || (localName == "Glyphs" && localName2 == "FontUri")))
                    {
                        if (localName2 == "ImageSource")
                        {
                            text = ProcessImageSourceAttribute(part.Uri, text);
                        }
                        else
                        {
                            text = MakeRelativeUri(part.Uri, text);
                        }
                    }
                    float[] array;
                    Uri partUri;
                    string colorProfileUri;
                    if (((localName == "SolidColorBrush" && localName2 == "Color") || (localName == "GradientStop" && localName2 == "Color") || (localName == "Path" && localName2 == "Fill") || (localName == "Glyphs" && localName2 == "Fill") || (localName == "Path" && localName2 == "Stroke")) && text.StartsWith("ContextColor", StringComparison.OrdinalIgnoreCase) && ParseContextColor(part.Uri, text, out array, out partUri, out colorProfileUri) && _package.PartExists(partUri))
                    {
                        PackagePart part2 = _package.GetPart(partUri);
                        if (part2.ContentType == "application/vnd.ms-color.iccprofile")
                        {
                            int num = 0;
                            using (Stream stream = part2.GetStream())
                            {
                                num = ICCHelper.GetColorProfileChannelCount(stream);
                            }
                            if (num > 0)
                            {
                                if (_to == XpsType.MSXPS && array.Length < 4)
                                {
                                    text = CreateContextColor(array, 3, colorProfileUri);
                                }
                                else if (_to == XpsType.OpenXPS && num < 3 && HasPaddingZeroes(array, num))
                                {
                                    text = CreateContextColor(array, num, colorProfileUri);
                                }
                            }
                        }
                    }
                    writer.WriteAttributeString(reader.Prefix, localName2, _xpsConstants.ConvertNamespace(reader.NamespaceURI), text);
                }
                if (isEmptyElement)
                {
                    writer.WriteEndElement();
                    return;
                }
            }
            else
            {
                CopyXmlNode(reader, writer);
            }
        }

        private void GetXmlEncoding(PackagePart part, out Encoding encoding, out bool hasXmlDeclaration)
        {
            using (Stream stream = part.GetStream())
            {
                using (XmlTextReader xmlTextReader = new XmlTextReader(stream))
                {
                    xmlTextReader.Read();
                    encoding = xmlTextReader.Encoding;
                    hasXmlDeclaration = (xmlTextReader.NodeType == XmlNodeType.XmlDeclaration);
                }
            }
        }

        private string MakeRelativeUri(Uri parentUri, string uri)
        {
            string result = uri;
            Uri uri2 = new Uri(uri, UriKind.RelativeOrAbsolute);
            if (!uri2.IsAbsoluteUri)
            {
                Uri targetPartUri = PackUriHelper.ResolvePartUri(parentUri, uri2);
                result = PackUriHelper.GetRelativeUri(parentUri, targetPartUri).ToString();
            }
            return result;
        }

        private string ProcessImageSourceAttribute(Uri parentUri, string imageSource)
        {
            string result = imageSource;
            string[] array = ParseImageSourceSyntax(imageSource.Trim());
            if (array.Length == 1)
            {
                result = MakeRelativeUri(parentUri, imageSource);
            }
            else if (array.Length == 2)
            {
                result = string.Concat(new string[]
                {
                    "{ColorConvertedBitmap ",
                    MakeRelativeUri(parentUri, array[0]),
                    " ",
                    MakeRelativeUri(parentUri, array[1]),
                    "}"
                });
            }
            else
            {
                Console.WriteLine("Invalid ImageSource found");
            }
            return result;
        }

        private string[] ParseImageSourceSyntax(string imageReference)
        {
            if (imageReference.StartsWith("{ColorConvertedBitmap", StringComparison.OrdinalIgnoreCase))
            {
                string[] array = imageReference.Split(new char[]
                {
                    ' '
                });
                if (array.Length < 3)
                {
                    Console.WriteLine("Invalid format in ImageSource with external color profile");
                }
                string[] array2 = new string[2];
                array2[0] = array[1];
                if (array[2].EndsWith("}", StringComparison.OrdinalIgnoreCase))
                {
                    array2[1] = array[2].Remove(array[2].Length - 1, 1);
                }
                else
                {
                    array2[1] = array[2];
                }
                return array2;
            }
            return new string[]
            {
                imageReference
            };
        }

        private bool ParseContextColor(Uri parentUri, string attributeValue, out float[] colorValues, out Uri profileUri, out string originalProfileUri)
        {
            profileUri = null;
            colorValues = null;
            originalProfileUri = null;
            CultureInfo provider = new CultureInfo("en-US");
            string text = attributeValue.Substring("ContextColor".Length);
            text = text.Trim();
            string[] array = text.Split(new char[]
            {
                ' '
            });
            if (array.Length < 2)
            {
                return false;
            }
            text = text.Substring(array[0].Length);
            string[] array2 = text.Split(new char[]
            {
                ',',
                ' '
            }, StringSplitOptions.RemoveEmptyEntries);
            int num = array2.Length;
            colorValues = new float[num];
            int num2 = 0;
            for (int i = 0; i < num; i++)
            {
                if (!float.TryParse(array2[num2++], NumberStyles.Number, provider, out colorValues[i]))
                {
                    return false;
                }
            }
            originalProfileUri = array[0];
            try
            {
                Uri targetUri = new Uri(originalProfileUri, UriKind.Relative);
                profileUri = PackUriHelper.ResolvePartUri(parentUri, targetUri);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        private bool HasPaddingZeroes(float[] colorValues, int colorProfileChannelsCount)
        {
            bool result = false;
            if (colorValues.Length - 1 > colorProfileChannelsCount)
            {
                result = true;
                for (int i = colorProfileChannelsCount + 1; i < colorValues.Length; i++)
                {
                    if (colorValues[i] != 0f)
                    {
                        result = false;
                    }
                }
            }
            return result;
        }

        private string CreateContextColor(float[] colorValues, int colorProfileChannelsCount, string colorProfileUri)
        {
            StringBuilder stringBuilder = new StringBuilder("ContextColor " + colorProfileUri + " ");
            for (int i = 0; i <= colorProfileChannelsCount; i++)
            {
                if (i < colorValues.Length)
                {
                    stringBuilder.Append(colorValues[i].ToString("0.0", CultureInfo.InvariantCulture));
                }
                else
                {
                    stringBuilder.Append("0.0");
                }
                if (i < colorProfileChannelsCount)
                {
                    stringBuilder.Append(", ");
                }
            }
            return stringBuilder.ToString();
        }

        private void CopyXmlNode(XmlReader reader, XmlWriter writer)
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Text:
                    writer.WriteString(reader.Value);
                    return;
                case XmlNodeType.CDATA:
                    writer.WriteCData(reader.Value);
                    return;
                case XmlNodeType.EntityReference:
                    writer.WriteEntityRef(reader.Name);
                    return;
                case XmlNodeType.Entity:
                case XmlNodeType.Document:
                case XmlNodeType.DocumentFragment:
                case XmlNodeType.Notation:
                case XmlNodeType.EndEntity:
                    break;
                case XmlNodeType.ProcessingInstruction:
                case XmlNodeType.XmlDeclaration:
                    writer.WriteProcessingInstruction(reader.Name, reader.Value);
                    return;
                case XmlNodeType.Comment:
                    writer.WriteComment(reader.Value);
                    return;
                case XmlNodeType.DocumentType:
                    writer.WriteDocType(reader.Name, reader.GetAttribute("PUBLIC"), reader.GetAttribute("SYSTEM"), reader.Value);
                    return;
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    writer.WriteWhitespace(reader.Value);
                    return;
                case XmlNodeType.EndElement:
                    writer.WriteFullEndElement();
                    break;
                default:
                    return;
            }
        }

        private bool IsXmlPart(string contentType)
        {
            return _xmlPartContentTypes.Contains(contentType.ToLowerInvariant());
        }

        private bool IsPackageLevelRelationshipsPart(PackagePart part)
        {
            return part.ContentType.Equals("application/vnd.openxmlformats-package.relationships+xml", StringComparison.OrdinalIgnoreCase);
        }

        private void CollectSignatureDefinitions()
        {
            _signatureDefinitions = new List<PackagePart>();
            foreach (PackagePart packagePart in _package.GetParts())
            {
                if (!IsPackageLevelRelationshipsPart(packagePart))
                {
                    foreach (PackageRelationship packageRelationship in packagePart.GetRelationshipsByType(_xpsConstants.SigDefSourceRT))
                    {
                        _signatureDefinitions.Add(_package.GetPart(packageRelationship.TargetUri));
                    }
                }
            }
        }

        private void FixSignatureDefinitionsContentType()
        {
            foreach (PackagePart packagePart in _signatureDefinitions)
            {
                Uri uri = packagePart.Uri;
                CompressionOption compressionOption = packagePart.CompressionOption;
                using (Stream stream = packagePart.GetStream())
                {
                    byte[] array = new byte[stream.Length];
                    int count = stream.Read(array, 0, array.Length);
                    _package.DeletePart(uri);
                    using (Stream stream2 = _package.CreatePart(uri, _xpsConstants.SigDefDestinationCT, compressionOption).GetStream())
                    {
                        stream2.Write(array, 0, count);
                    }
                }
            }
        }

        private bool IsJpegXRSupported()
        {
            return Environment.OSVersion.Version.Major >= 6 && Environment.OSVersion.Version.Minor >= 1 && Environment.OSVersion.Version.Build > 7600;
        }

        private void ConvertHDPhoto(PackagePart part)
        {
            using (Stream stream = part.GetStream())
            {
                BitmapDecoder bitmapDecoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                WmpBitmapEncoder wmpBitmapEncoder = new WmpBitmapEncoder();
                for (int i = 0; i < bitmapDecoder.Frames.Count; i++)
                {
                    BitmapFrame item = BitmapFrame.Create(bitmapDecoder.Frames[i]);
                    wmpBitmapEncoder.Frames.Add(item);
                }
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    wmpBitmapEncoder.Save(memoryStream);
                    stream.SetLength(0L);
                    memoryStream.Seek(0L, SeekOrigin.Begin);
                    memoryStream.CopyTo(stream);
                }
            }
        }

        public static string EscapeLoggerString(string message)
        {
            return message.Replace("{", "{{").Replace("}", "}}");
        }
    }
}
