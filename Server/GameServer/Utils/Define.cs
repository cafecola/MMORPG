
namespace GameServer
{
    public class Define
    {
        public const char MAP_TOOL_WALL = '0';
        public const char MAP_TOOL_NONE = '1';

        public const float TILE_WIDTH = 2.0f; // x = 2
        public const float TILE_HEIGHT = 1.0f; // y = 1

        public const int RANDOM_WEIGHT_SCALE = 10000; 

        public static readonly float DIAGONAL_DISTANCE = (float)Math.Sqrt(TILE_WIDTH * 0.5f * TILE_WIDTH * 0.5f + TILE_HEIGHT * 0.5f * TILE_HEIGHT * 0.5f);

    }
}
