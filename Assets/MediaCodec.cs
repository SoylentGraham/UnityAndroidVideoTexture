using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

//	http://bigflake.com/mediacodec/ExtractMpegFramesTest_egl14.java.txt

public class MediaCodec : MonoBehaviour {

	public string mFilename = "stadium.ogg";
	private string mLog = "Init\n";

	// Use this for initialization
	void Start () {

		mLog += "Hello world \n";

		try
		{
			//	copy file
#if UNITY_EDITOR
			String StreamingFilename = "file:" + System.IO.Path.Combine(Application.streamingAssetsPath, mFilename);
#else
			String StreamingFilename = System.IO.Path.Combine(Application.streamingAssetsPath, mFilename);
#endif
			String PersistFilename = System.IO.Path.Combine(Application.persistentDataPath, mFilename);
			mLog += "Loading www(" + StreamingFilename + ")\n";
			WWW StreamingFile = new WWW(StreamingFilename);
			while ( !StreamingFile.isDone )
			{
				mLog += "loading...." + StreamingFile.bytesDownloaded + "\n";
			}
			if ( StreamingFile.error != null )
			{
				mLog += "www error: " + StreamingFile.error + "\n";
				return;
			}
			mLog += "Loaded: " +  StreamingFile.bytesDownloaded + "\n";

			mLog += "Writing to persistent: " + PersistFilename + "\n";
			System.IO.File.WriteAllBytes ( PersistFilename, StreamingFile.bytes );

			mLog += "create extractor...\n";
			AndroidJavaObject extractor = new AndroidJavaObject("android.media.MediaExtractor");
			mLog += "extractor: " + extractor.ToString() + "\n";
			String Filename = PersistFilename;

			if (!System.IO.File.Exists(Filename))
				mLog += "File does not exist: " + Filename+"\n";
			else {
				var FileInfo = new System.IO.FileInfo(Filename);
				mLog += "file exists: " + FileInfo.Length + "\n";
			}

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

			mLog += "setDataSource " + Filename + "\n";
			extractor.Call("setDataSource",Filename);

			int TrackCount = extractor.Call<int>("getTrackCount");
			mLog += "tracks: " + TrackCount + "\n";
			int TrackIndex = 0;
			mLog += "selectTrack " + TrackIndex + "\n";
			extractor.Call ("selectTrack",TrackIndex);
		
			mLog += "getTrackFormat " + TrackIndex + "\n";
			AndroidJavaObject format = extractor.Call<AndroidJavaObject>("getTrackFormat",TrackIndex);
		
			String FormatDescription = format.Call<String>("toString");
			mLog += "format.toString() = " + FormatDescription + "\n";

			//	sdk literals
			const String KEY_MIME = "mime";
			const String KEY_WIDTH = "width";
			const String KEY_HEIGHT = "height";
			String Mime = format.Call<string>("getString", KEY_MIME );
			int Width = format.Call<int>("getInteger", KEY_WIDTH );
			int Height = format.Call<int>("getInteger", KEY_HEIGHT );
			mLog += "video=" + Mime + "; " + Width + "x" + Height + "\n";
		}
		catch ( Exception e )
		{
			mLog += e.Message + "\n";
		}
		//jo.Call<android.media.MediaCodec>("createDecoderByType");

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
		}
}
