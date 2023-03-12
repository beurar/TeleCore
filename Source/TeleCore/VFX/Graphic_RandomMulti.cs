using Verse;

namespace TeleCore
{
	public class Graphic_RandomMulti : Graphic
    {
	    private Graphic_Multi[] subGraphics;
        public const string NorthSuffix = "_north";
        public const string SouthSuffix = "_south";
        public const string EastSuffix = "_east";
        public const string WestSuffix = "_west";
        public const string MaskSuffix = "_m";
        
        public override void Init(GraphicRequest req)
        {
	        data = req.graphicData;
	        this.path = req.path;
	        this.maskPath = req.maskPath;
	        this.color = req.color;
	        this.colorTwo = req.colorTwo;
	        this.drawSize = req.drawSize;
        }
    }
}