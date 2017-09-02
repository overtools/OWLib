namespace DataTool.Flag {
    public abstract class ICLIFlags {
        public string[] Positionals;

        public abstract bool Validate();
    }
}
