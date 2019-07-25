using System.Collections.Generic;

namespace Microsoft.XpsConverter
{
    internal class XpsConstants
	{
        private string[,] _rels;
        private string[,] _ns;
        public const string CONTEXTCOLOR = "ContextColor";
        public const string CT_ICC = "application/vnd.ms-color.iccprofile";
        public const string CT_WMP = "image/vnd.ms-photo";
        private Dictionary<string, string> _relationships;
        private Dictionary<string, string> _namespaces;

        public string SigDefSourceRT { get; }

        public string SigDefDestinationCT { get; }

        public XpsConstants(XpsType convertFrom, XpsType convertTo) : base()
		{
			string[,] array = new string[8, 2];
			array[0, 0] = "http://schemas.microsoft.com/xps/2005/06/signature-definitions";
			array[0, 1] = "http://schemas.openxps.org/oxps/v1.0/signature-definitions";
			array[1, 0] = "http://schemas.microsoft.com/xps/2005/06/discard-control";
			array[1, 1] = "http://schemas.openxps.org/oxps/v1.0/discard-control";
			array[2, 0] = "http://schemas.microsoft.com/xps/2005/06/documentstructure";
			array[2, 1] = "http://schemas.openxps.org/oxps/v1.0/documentstructure";
			array[3, 0] = "http://schemas.microsoft.com/xps/2005/06/printticket";
			array[3, 1] = "http://schemas.openxps.org/oxps/v1.0/printticket";
			array[4, 0] = "http://schemas.microsoft.com/xps/2005/06/required-resource";
			array[4, 1] = "http://schemas.openxps.org/oxps/v1.0/required-resource";
			array[5, 0] = "http://schemas.microsoft.com/xps/2005/06/restricted-font";
			array[5, 1] = "http://schemas.openxps.org/oxps/v1.0/restricted-font";
			array[6, 0] = "http://schemas.microsoft.com/xps/2005/06/fixedrepresentation";
			array[6, 1] = "http://schemas.openxps.org/oxps/v1.0/fixedrepresentation";
			array[7, 0] = "http://schemas.microsoft.com/xps/2005/06/storyfragments";
			array[7, 1] = "http://schemas.openxps.org/oxps/v1.0/storyfragments";
			_rels = array;
			string[,] array2 = new string[5, 2];
			array2[0, 0] = "http://schemas.microsoft.com/xps/2005/06/discard-control";
			array2[0, 1] = "http://schemas.openxps.org/oxps/v1.0/discard-control";
			array2[1, 0] = "http://schemas.microsoft.com/xps/2005/06/documentstructure";
			array2[1, 1] = "http://schemas.openxps.org/oxps/v1.0/documentstructure";
			array2[2, 0] = "http://schemas.microsoft.com/xps/2005/06";
			array2[2, 1] = "http://schemas.openxps.org/oxps/v1.0";
			array2[3, 0] = "http://schemas.microsoft.com/xps/2005/06/resourcedictionary-key";
			array2[3, 1] = "http://schemas.openxps.org/oxps/v1.0/resourcedictionary-key";
			array2[4, 0] = "http://schemas.microsoft.com/xps/2005/06/signature-definitions";
			array2[4, 1] = "http://schemas.openxps.org/oxps/v1.0/signature-definitions";
			_ns = array2;

			_relationships = new Dictionary<string, string>();
			_namespaces = new Dictionary<string, string>();
			for (int i = 0; i < _rels.GetLength(0); i++)
			{
				_relationships.Add(_rels[i, (int)convertFrom], _rels[i, (int)convertTo]);
			}
			for (int j = 0; j < _ns.GetLength(0); j++)
			{
				_namespaces.Add(_ns[j, (int)convertFrom], _ns[j, (int)convertTo]);
			}
			if (convertFrom == XpsType.MSXPS)
			{
				SigDefSourceRT = "http://schemas.microsoft.com/xps/2005/06/signature-definitions";
			}
			else
			{
				SigDefSourceRT = "http://schemas.openxps.org/oxps/v1.0/signature-definitions";
			}
			if (convertTo == XpsType.MSXPS)
			{
				SigDefDestinationCT = "application/xml";
				return;
			}
			SigDefDestinationCT = "application/vnd.ms-package.xps-signaturedefinitions+xml";
		}

		public string ConvertRelationshipType(string relationshipType)
		{
			if (_relationships.ContainsKey(relationshipType))
			{
				return _relationships[relationshipType.ToLowerInvariant()];
			}
			return relationshipType;
		}

		public string ConvertNamespace(string namespaceURI)
		{
			if (_namespaces.ContainsKey(namespaceURI))
			{
				return _namespaces[namespaceURI.ToLowerInvariant()];
			}
			return namespaceURI;
		}
    }
}
