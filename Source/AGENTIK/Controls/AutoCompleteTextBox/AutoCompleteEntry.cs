namespace AGENTIK.Controls.AutoCompleteTextBox
{
    public class AutoCompleteEntry
    {
        private string[] _keywordStrings;
        private string _displayString;

        public string[] KeywordStrings
        {
            get
            {
                if (_keywordStrings == null)
                {
                    _keywordStrings = new string[] { _displayString };
                }
                return _keywordStrings;
            }
        }

        public string DisplayName
        {
            get { return _displayString; }
            set { _displayString = value; }
        }

        public AutoCompleteEntry(string name, params string[] keywords)
        {
            _displayString = name;
            _keywordStrings = keywords;
        }

        public override string ToString()
        {
            return _displayString;
        }
    }
}
