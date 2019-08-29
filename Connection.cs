//using OracleDataManager;
using System;
using System.Collections;
using System.Data;
using System.Data.OleDb;
using System.Threading;
using DLRUtilityCollection;

namespace OleDBDataManager
{
	/// <summary>
	/// Summary description for Connection.
	/// </summary>
	public class Connection
	{
		private static Connection db_connect = null;
		private string dBConnectString = "";
		private string dBConnectStringBase = "";
		private bool needLogIn = true;
		private bool debug = false;
		private User userObject = null;


		public string DBConnectString
		{
			get { return dBConnectString; }
			set { dBConnectString = value; }
		}

		public bool DEBUG
		{
			get { return debug; }
			set { debug = value; }
		}

		public bool NeedLogIn
		{
			get
			{ //needLogIn = FullyFormedConnectString();
				return needLogIn;
			}
			set { needLogIn = value; }
		}

		public User UserObject
		{
			get { return userObject; }
			set { userObject = value; }
		}


		/// <summary>
		/// In the app.config file :
		///		for the connect string - UserID comes before dlr_debug and
		///								 if you have dlr_debug, you need to include app_path
		///		ie: <add key="connect" value="item1=...;item2=...; User ID=apps;dlr_debug;"/>
		///			<add key="app_path" value="C:\...\...\MyApp"/>
		/// </summary>
		private Connection()
		{
			ConfigData.CreateInstance();
			dBConnectStringBase = ConfigData.GetValue("connect"); //nwlrl_connect  **!** also uncomment the password & User lines below
			if (dBConnectStringBase.IndexOf("dlr_debug") > 0)
			{ //if 'debug' then provide the user name and pw
				string[] cnctStr = dBConnectStringBase.Split(';');
				string userName = "";
				string connectItems = "";
                string app_path = "";
				string pWord = "";
				app_path = app_path.Length > 0 ? app_path : ConfigData.GetValue("app_path").ToString();				
				dBConnectStringBase = "";
				needLogIn = false;
				for (int x = 0; x < cnctStr.Length; x++)
				{
					if (cnctStr[x].Length > 0)
					{
						connectItems = cnctStr[x].ToString().Trim().Substring(0, 6);
						if (connectItems.Equals("UserID") || connectItems.Equals("User I"))
						{
							string[] uid = cnctStr[x].ToString().Split('=');
							userName = uid[1];
						}
						if (cnctStr[x].ToString().Trim().Equals("dlr_debug"))
						{
							if (userName.Length > 0)
							{
								Crypto cryptLogIn = new Crypto(app_path);
								cryptLogIn.Dbug = true;

								pWord = cryptLogIn.Decrypt(cryptLogIn.ReadFromFile(), userName).ToString();
								if (pWord.Length > 0)
								{
									dBConnectStringBase += "User ID=" + userName + ";password=" + pWord;
									userObject = new User(userName, pWord);
								}
								else
									needLogIn = true;
							}
							else
							{
								needLogIn = true;
								continue;
							}
						}
						else if (cnctStr[x].ToString().Trim().Length > 0)
						{ //catches any empty split elements 
							dBConnectStringBase += cnctStr[x].ToString() + ";";
						}
					}
				}
				dBConnectString = dBConnectStringBase;
				debug = true;
			}
		}

		public static Connection GetInstance()
		{
			if (db_connect == null)
			{
				CreateInstance();
			}
			return db_connect;
		}

		private static void CreateInstance()
		{
			Mutex configMutex = new Mutex();
			configMutex.WaitOne();
			db_connect = new Connection();
			configMutex.ReleaseMutex();
		}

//		private bool FullyFormedConnectString() {
//			bool rtnValu = needLogIn;
//			if(dBConnectString.IndexOf("password") > 0)
//				rtnValu = false;
//			return rtnValu;
//		}

		public static void DeleteInstance()
		{
			db_connect = null;
		}

		public bool VerifyUserLogin(ref string errorMsg)
		{
			bool rtnValu = false;
			if (userObject != null)
			{
				if (userObject.Name.Length > 0 && userObject.Word.Length > 0)
				{
					ODMDataFactory ODMDataSetFactory = new ODMDataFactory();
					ODMRequest Request = new ODMRequest();
					dBConnectString = dBConnectStringBase + ";" + "User ID=" + userObject.Name + ";" + "password=" + userObject.Word + ";";
					string strMsg = "";

					//attempt a db connection with this connect string										
					BuildRequest(ref Request);
					ArrayList Message = new ArrayList();
					try
					{
						Message = ODMDataSetFactory.ExecuteNonQueryOutParams(ref Request);
						if (Message.Count > 0)
						{
							strMsg = ((OleDbParameter) Message[0]).Value.ToString();
							if (strMsg.StartsWith("Error")) //"ERROR" is returned by the stored proc
								throw new DatabaseException("An Error Was Encountered During a Database Operation:" + Environment.NewLine + Request.Command.ToString() + " frmCodes:PostToDB");
						}
						rtnValu = true;
						needLogIn = false;
					}
					catch (DatabaseException dbx)
					{
						errorMsg = "Login Failed:" + Environment.NewLine;
						if (Request.DbException.Message.Length > 0)
							errorMsg += Request.DbException.Message;
						else if (dbx.Message.Length > 0)
							errorMsg += dbx.Message;
						else
							errorMsg += "Unspecified database Error";
					}
				}
			}
			return rtnValu;
		}

		private void BuildRequest(ref ODMRequest Request)
		{
			OleDbParameter odbParam = new OleDbParameter();
			//add the only INOUT param
			odbParam.OleDbType = OleDbType.VarChar;
			odbParam.ParameterName = "p_message";
			odbParam.Size = 16;
			odbParam.Value = string.Empty;
			odbParam.Direction = ParameterDirection.InputOutput;
			Request.ParamCollection.Add(odbParam);

			Request.Command = GDS.Package + "Check_User_Login";
			Request.ConnectString = DBConnectString;
			Request.CommandType = CommandType.StoredProcedure;
		}

		public void LogOut()
		{
			db_connect = null;
		}

	}
}
