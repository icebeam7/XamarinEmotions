using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Text;
using Android.Widget;
using Java.IO;

namespace XamarinEmotions
{
	[Activity(Label = "XamarinEmotions", MainLauncher = true, Icon = "@mipmap/icon", ScreenOrientation = ScreenOrientation.Portrait)]
	public class MainActivity : Activity
	{
		File archivo;
		File directorio;
		Bitmap bmp;

		ImageView imgFoto, imgCanvas;
		Button btnTomarFoto, btnSeleccionarFoto;
		TextView txvResultado;

		private bool ModoCaptura = true;

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			SetContentView(Resource.Layout.Main);

			if (PuedeTomarFotos())
			{
				CrearDirectorioFotos();

				btnTomarFoto = FindViewById<Button>(Resource.Id.btnTomarFoto);
				btnTomarFoto.Click += btnTomarFoto_Click;

				btnSeleccionarFoto = FindViewById<Button>(Resource.Id.btnSeleccionarFoto);
				btnSeleccionarFoto.Click += btnSeleccionarFoto_Click;

				imgFoto = FindViewById<ImageView>(Resource.Id.imgFoto);
				imgCanvas = FindViewById<ImageView>(Resource.Id.imgCanvas);

				txvResultado = FindViewById<TextView>(Resource.Id.txvResultado);
			}
		}

		private void CrearDirectorioFotos()
		{
			directorio = new File(Android.OS.Environment.GetExternalStoragePublicDirectory(
				Android.OS.Environment.DirectoryPictures), "XamarinEmotions");

			if (!directorio.Exists())
				directorio.Mkdirs();
		}

		private bool PuedeTomarFotos()
		{
			Intent intent = new Intent(MediaStore.ActionImageCapture);
			IList<ResolveInfo> lstActividades =
				PackageManager.QueryIntentActivities(intent, PackageInfoFlags.MatchDefaultOnly);
			return lstActividades != null && lstActividades.Count > 0;
		}

		private void btnSeleccionarFoto_Click(object sender, EventArgs eventArgs)
		{
			Intent intent = new Intent();
			intent.SetType("image/*");
			intent.SetAction(Intent.ActionGetContent);
			StartActivityForResult(Intent.CreateChooser(intent, "Elige una foto"), 1);
		}

		private void btnTomarFoto_Click(object sender, EventArgs eventArgs)
		{
			if (ModoCaptura == true)
			{
				Intent intent = new Intent(MediaStore.ActionImageCapture);
				archivo = new File(directorio, String.Format("foto_{0}.jpg", Guid.NewGuid()));
				intent.PutExtra(MediaStore.ExtraOutput, Android.Net.Uri.FromFile(archivo));
				StartActivityForResult(intent, 0);
			}
			else
			{
				imgFoto.SetImageBitmap(null);
				imgCanvas.SetImageDrawable(null);

				if (bmp != null)
				{
					bmp.Recycle();
					bmp.Dispose();
					bmp = null;
				}

				btnTomarFoto.Text = "Tomar Foto";
				txvResultado.Text = "";
				ModoCaptura = true;
			}
		}

		protected override async void OnActivityResult(int requestCode, Result resultCode, Intent data)
		{
			base.OnActivityResult(requestCode, resultCode, data);

			txvResultado.Text = "Analizando...";
			imgCanvas.SetImageDrawable(null);

			switch (requestCode)
			{
				case 0:
					try
					{
						bmp = BitmapHelper.Obtener_Rotar(archivo.Path);
					}
					catch (Exception ex)
					{
						txvResultado.Text = ex.Message;
					}
					finally
					{
						btnTomarFoto.Text = "Reiniciar";
						ModoCaptura = false;
					}
					break;
				case 1:
					try
					{
						if (resultCode == Result.Ok)
						{
							string uri = ObtenerUriReal(data.Data, this);
							bmp = BitmapHelper.Obtener_Rotar(uri);
						}
					}
					catch (Exception ex)
					{
						txvResultado.Text = ex.Message;
					}
					break;
			}

			bmp = Bitmap.CreateScaledBitmap(bmp, 2000, (int)(2000 * bmp.Height / bmp.Width), false);
			imgFoto.SetImageBitmap(bmp);

			using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
			{
				bmp.Compress(Bitmap.CompressFormat.Jpeg, 90, stream);
				stream.Seek(0, System.IO.SeekOrigin.Begin);

				var emociones = await Emociones.ObtenerEmociones(stream);
				txvResultado.Text = "---Análisis de Emociones---";

				if (emociones != null)
					DibujarCanvas(emociones);
				else
					txvResultado.Text = "Error: No se detectó una cara";
			}
		}

		void DibujarCanvas(Dictionary<string, float> emociones)
		{
			imgCanvas.SetImageDrawable(null);

			Bitmap pallet = Bitmap.CreateBitmap(900, 600, Bitmap.Config.Argb8888);
			Canvas canvas = new Canvas(pallet);

			Paint paint = new Paint();
			paint.Color = Color.Black;
			paint.SetStyle(Paint.Style.Fill);
			canvas.DrawPaint(paint);

			int y = 50;

			foreach (var emocion in emociones)
			{
				TextPaint textE = new TextPaint(PaintFlags.AntiAlias);
				textE.TextSize = 25f;
				textE.Color = Color.White;
				canvas.DrawText(emocion.Key, 10, y, textE);
				canvas.DrawText(emocion.Value.ToString("P4"), 610, y, textE);

				Paint green = new Paint(PaintFlags.AntiAlias);
				green.Color = Color.Green;
				green.SetStyle(Paint.Style.FillAndStroke);

				Paint white = new Paint(PaintFlags.AntiAlias);
				white.Color = Color.White;
				white.SetStyle(Paint.Style.FillAndStroke);

				float middle = (canvas.Width - 500) * emocion.Value;
				canvas.DrawRect(200, y-25, canvas.Width - 300, y, white);
				canvas.DrawRect(200, y-25, 200 + middle, y, green);

				y += 60;
			}

			imgCanvas.SetImageBitmap(pallet);
		}

		private string ObtenerUriReal(Android.Net.Uri contentURI, Activity activity)
		{
			var cursor = activity.ContentResolver.Query(contentURI, null, null, null, null);

			if (cursor == null)
			{
				return contentURI.Path;
			}
			else
			{
				cursor.MoveToFirst();
				int idx = cursor.GetColumnIndex(MediaStore.Images.ImageColumns.Data);
				return cursor.GetString(idx);
			}
		}
	}
}