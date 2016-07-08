using Microsoft.ProjectOxford.Emotion;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace XamarinEmotions
{
	public static class Emociones
	{
		static string key = "9c8e649e736b4b079bb370c3b668dfba";

		public static async Task<Dictionary<string, float>> ObtenerEmociones(Stream stream)
		{
			EmotionServiceClient cliente = new EmotionServiceClient(key);
			var emociones = await cliente.RecognizeAsync(stream);

			if (emociones == null || emociones.Count() == 0)
				return null;

			return emociones[0].Scores.ToRankedList().ToDictionary(x => x.Key, x => x.Value);
		}
	}
}

