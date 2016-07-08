using Android.Graphics;
using Android.Media;

namespace XamarinEmotions
{
	public static class BitmapHelper
	{
		public static Bitmap Obtener_Rotar(string archivo, Bitmap bmp = null)
		{
			bmp = BitmapFactory.DecodeFile(archivo);

			using (Matrix mtx = new Matrix())
			{
				if (Android.OS.Build.Product.Contains("Emulator"))
					mtx.PreRotate(90);
				else
				{
					ExifInterface exif = new ExifInterface(archivo);
					var orientacion = (Orientation)exif.GetAttributeInt(ExifInterface.TagOrientation, (int)Orientation.Normal);

					switch (orientacion)
					{
						case Orientation.Rotate90: mtx.PreRotate(90); break;
						case Orientation.Rotate180: mtx.PreRotate(180); break;
						case Orientation.Rotate270: mtx.PreRotate(270); break;
						case Orientation.Normal: break;
						default: break;
					}
				}

				if (mtx != null)
					bmp = Bitmap.CreateBitmap(bmp, 0, 0, bmp.Width, bmp.Height, mtx, false);
			}

			return bmp;
		}
	}
}