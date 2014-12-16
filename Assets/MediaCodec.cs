using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

//	http://bigflake.com/mediacodec/ExtractMpegFramesTest_egl14.java.txt

class TVideoDecoder
{
	public AndroidJavaObject	Surface;
	public AndroidJavaObject	SurfaceTexture;
	public AndroidJavaObject	Decoder;
	public Texture2D			Texture;
};

class TMediaFormat
{
	public int		Width = 0;
	public int		Height = 0;
	public String	Mime = "";
	public AndroidJavaObject	Format = null;
};

public class MediaCodec : MonoBehaviour {

	public string mFilename = "stadium.ogg";
	private static string mLog = "Init\n";

	[NonSerialized]
	private TVideoDecoder mDecoder;

	static void Log(String Message)	{
			mLog += Message + "\n";
	}

	static AndroidJavaObject LoadExtractor(String AssetFilename)
	{
		try
		{
			//	copy file
			#if UNITY_EDITOR
			String StreamingFilename = "file:" + System.IO.Path.Combine(Application.streamingAssetsPath, AssetFilename);
			#else
			String StreamingFilename = System.IO.Path.Combine(Application.streamingAssetsPath, AssetFilename);
			#endif
			String PersistFilename = System.IO.Path.Combine(Application.persistentDataPath, AssetFilename);
			Log("Loading www(" + StreamingFilename + ")");
			WWW StreamingFile = new WWW(StreamingFilename);
			while ( !StreamingFile.isDone )
			{
				Log("loading...." + StreamingFile.bytesDownloaded );
			}
			if ( StreamingFile.error != null )
			{
				Log ("www error: " + StreamingFile.error );
				return null;
			}

			mLog += "Writing to persistent: " + PersistFilename + "\n";
			System.IO.File.WriteAllBytes ( PersistFilename, StreamingFile.bytes );
			
			AndroidJavaObject extractor = new AndroidJavaObject("android.media.MediaExtractor");
			Log ("extractor: " + extractor.ToString() );
			String LoadFilename = PersistFilename;
			
			if (!System.IO.File.Exists(LoadFilename))
			{
				Log ("File does not exist: " + LoadFilename );
				return null;
			}

			var FileInfo = new System.IO.FileInfo(LoadFilename);
			Log("file exists: " + FileInfo.Length);
			
			/*
			//	make ram file
			mLog += "making ram file \n";
			AndroidJavaObject RamFile = new AndroidJavaObject("android.io.RandomAccessFile", "myfile", "wb" );
			RamFile.Call("write", StreamingFile.bytes );
			AndroidJavaObject RamFileDescriptor = RamFile.Call<AndroidJavaObject>("getFD");
			mLog += "setDataSource RAMFILE" + Filename + "\n";
			extractor.Call("setDataSource",RamFileDescriptor);
			extractor.Call("setDataSource",Filename);
*/
			
			mLog += "setDataSource " + LoadFilename + "\n";
			extractor.Call("setDataSource",LoadFilename);

			int TrackCount = extractor.Call<int>("getTrackCount");
			mLog += "tracks: " + TrackCount + "\n";
			int TrackIndex = 0;
			mLog += "selectTrack " + TrackIndex + "\n";
			extractor.Call ("selectTrack",TrackIndex);
			return extractor;
		}
		catch ( Exception e )
		{
			Log ("Exception: " + e.Message );
			return null;
		}
	}

	static int GetTrack(AndroidJavaObject Extractor,String Filter="video/")
	{
		return 0;
	}


	static TMediaFormat GetFormat(AndroidJavaObject Extractor)
	{
		int TrackCount = Extractor.Call<int>("getTrackCount");
		Log ("tracks: " + TrackCount);
		int TrackIndex = GetTrack (Extractor);
		Log ("selectTrack " + TrackIndex);
		Extractor.Call ("selectTrack",TrackIndex);
		
		Log ("getTrackFormat " + TrackIndex);
		try
		{
			TMediaFormat MediaFormat = new TMediaFormat();
			MediaFormat.Format = Extractor.Call<AndroidJavaObject>("getTrackFormat",TrackIndex);
			String FormatDescription = MediaFormat.Format.Call<String>("toString");
			Log("format.toString() = " + FormatDescription );
			
			//	sdk literals
			const String KEY_MIME = "mime";
			const String KEY_WIDTH = "width";
			const String KEY_HEIGHT = "height";

			MediaFormat.Mime = MediaFormat.Format.Call<string>("getString", KEY_MIME );
			MediaFormat.Width = MediaFormat.Format.Call<int>("getInteger", KEY_WIDTH );
			MediaFormat.Height = MediaFormat.Format.Call<int>("getInteger", KEY_HEIGHT );
			return MediaFormat;
		}
		catch ( Exception e )
		{
			Log ("GetFormatException: " + e.Message );
			return null;
		}
	}

	static TVideoDecoder CreateDecoder(AndroidJavaObject Extractor,TMediaFormat Format)
	{
		TVideoDecoder Decoder = new TVideoDecoder ();
		Decoder.Texture = new Texture2D (Format.Width, Format.Height);
		Decoder.Texture.SetPixel(0,0,Color.magenta);
		Decoder.Texture.Apply();

		try
		{
			int TextureId = Decoder.Texture.GetNativeTextureID();
			Decoder.SurfaceTexture = new AndroidJavaObject("android.graphics.SurfaceTexture", TextureId );
			Decoder.Surface = new AndroidJavaObject("android.view.Surface", Decoder.SurfaceTexture );
		}
		catch ( Exception e )
		{
			Log ("CreateDecoder::surface:: " + e.Message );
			return null;
		}

		try
		{
			Log ("Creating codec for " + Format.Mime );
			AndroidJavaClass MediaCodecClass = new AndroidJavaClass("android.media.MediaCodec");
			Decoder.Decoder = MediaCodecClass.CallStatic<AndroidJavaObject>("createDecoderByType", Format.Mime );

			//	"configure not found" when supplying surface....
			Decoder.Decoder.Call("configure", Format.Format, Decoder.Surface, null, 0);
			Decoder.Decoder.Call("start");
		}
		catch ( Exception e )
		{
			Log ("CreateDecoder::create decoder:: " + e.Message );
			return null;
		}

		return Decoder;
	}

	// Use this for initialization
	void Start () {

		AndroidJavaObject Extractor = LoadExtractor (mFilename);
		if (Extractor == null)
			return;

		TMediaFormat Format = GetFormat( Extractor );
		if (Format==null)
			return;
		Log ("Format of track is " + Format.Mime + " " + Format.Width + "x" + Format.Height);
		//jo.Call<android.media.MediaCodec>("createDecoderByType");

		mDecoder = CreateDecoder (Extractor, Format);

		DecodeFrame ();
	}

	void DecodeFrame()
	{
	}

	// Update is called once per frame
	void Update () {

	}

	void OnGUI()
	{
		GUI.skin.button.wordWrap = true;
		int Size = 60;
#if UNITY_EDITOR
		Size = 20;
#endif
		GUI.Label (new Rect (0, 0, Screen.width, Screen.height), "<size=" + Size + ">" + mLog + "</size>");


		if (mDecoder!=null && mDecoder.Texture) {
			int Width = mDecoder.Texture.width;
			int Height =  mDecoder.Texture.height;
			GUI.DrawTexture( new Rect ( Screen.width - Width, 0, Width, Height ), mDecoder.Texture );
		}

	}
}
