using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ADSCommon.Util
{
    public static class Vector2Extensions
    {
        public static Vector2 Normalized(this Vector2 v) => Vector2.Normalize(v);

        public static Vector2 Rotate(this Vector2 vector, float angleInRadians)
        {
            var cos = MathF.Cos(angleInRadians);
            var sin = MathF.Sin(angleInRadians);
            var x = vector.X * cos - vector.Y * sin;
            var y = vector.X * sin + vector.Y * cos;
            return new Vector2(x, y);
        }

        public static Vector2 PerpendicularClockwise(this Vector2 v)=> new Vector2(v.Y, -v.X);

        public static Vector2 TranslateRotateTranslate(this Vector2 vector, Vector2 translation, float angle)
        {
            // Translate the vector
            var translatedVector = vector + translation;
            // Rotate the translated vector
            var rotatedVector = translatedVector.Rotate(angle);
            // Translate the rotated vector back
            var finalVector = rotatedVector - translation;
            return finalVector;
        }

        public static float AngleTo(this Vector2 from, Vector2 to)
        {
            var angle = MathF.Atan2(to.Y, to.X) - MathF.Atan2(from.Y, from.X);
            if (angle > MathF.PI)
            {
                angle -= 2 * MathF.PI;
            }
            else if (angle <= -MathF.PI)
            {
                angle += 2 * MathF.PI;
            }
            return angle;
        }
    }
}
