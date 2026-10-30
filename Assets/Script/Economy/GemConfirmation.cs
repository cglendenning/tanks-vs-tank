namespace TreadShred.Economy
{
    /// <summary>The words on the dialog that confirms spending gems already held.</summary>
    public readonly struct GemConfirmation
    {
        public GemConfirmation(string title, string detail, string confirmButton, string cancelButton)
        {
            Title = title;
            Detail = detail;
            ConfirmButton = confirmButton;
            CancelButton = cancelButton;
        }

        public string Title { get; }
        public string Detail { get; }
        public string ConfirmButton { get; }
        public string CancelButton { get; }
    }
}
