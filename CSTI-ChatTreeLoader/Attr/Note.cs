using System;

namespace ChatTreeLoader.Attr
{
    [AttributeUsage(AttributeTargets.Field)]
    public class Note : Attribute
    {
        public string note;

        public Note(string note)
        {
            this.note = note;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class NoteEn : Attribute
    {
        public string note;

        public NoteEn(string note)
        {
            this.note = note;
        }
    }
}