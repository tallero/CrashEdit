using Crash;

namespace CrashEdit
{
    public sealed class OldSoundChunkController : EntryChunkController
    {
        public OldSoundChunkController(NSFController nsfcontroller,OldSoundChunk oldsoundchunk) : base(nsfcontroller,oldsoundchunk)
        {
            OldSoundChunk = oldsoundchunk;
            InvalidateNode();
            InvalidateNodeImage();
        }

        public override void InvalidateNode()
        {
            Node.Text = string.Format("Old Sound Chunk {0}",NSFController.NSF.Chunks.IndexOf(OldSoundChunk) * 2 + 1);
        }

        public override void InvalidateNodeImage()
        {
            Node.ImageKey = "bluej";
            Node.SelectedImageKey = "bluej";
        }

        public OldSoundChunk OldSoundChunk { get; }
    }
}
