using System;
using System.Collections;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
//using DLRUtilityCollection;

namespace OleDBDataManager
{
	/// <summary>
	/// Summary description for ODMDataFactory.
	/// </summary>
	public class ODMDataFactory {
		private DataSet ODMDataSet = null;
		private OleDbConnection conOLEDB = null;
		//private static LogWriter odmLog = null;
		private bool logEnabled = false;

		public OleDbConnection ConOledb {
			get { return conOLEDB; }
		}

		public bool LogEnabled
		{
			get { return logEnabled; }
			set { logEnabled = value; }
		}

		public ODMDataFactory() {}
		public ODMDataFactory(bool enableLog) {
			LogEnabled = enableLog ? true : false;
			//odmLog = LogEnabled ? LogWriter.GetInstance() : null;
		}

		public DataSet ExecuteDataSetBuild(ODMRequest Request) {
			return ExecuteDataSetBuild(ref Request);
		}

		public DataSet ExecuteDataSetBuild(ref ODMRequest Request) {
			string primaryKey = "";
			return ExecuteDataSetBuild(ref Request, primaryKey);
		}

		public DataSet ExecuteDataSetBuild(ref ODMRequest Request, string primaryKey) {
			if(LogEnabled)
				LogRequestParams(Request) ;
            if (conOLEDB != null && conOLEDB.State == ConnectionState.Open)
            {
                conOLEDB.Close();
                conOLEDB = null;
            }
            conOLEDB = new OleDbConnection();
			OleDbCommand cmdOLEDB = new OleDbCommand();
			OleDbParameter prmOLEDB = new OleDbParameter();
			//ODMRequest.Parameter oParam;
//			OleDbParameterCollection oledbParams;
			OleDbDataAdapter daOleDb = new OleDbDataAdapter();
			ODMDataSet = new DataSet();

			try {
				conOLEDB.ConnectionString = Request.ConnectString;
				conOLEDB.Open();
				cmdOLEDB.Connection = conOLEDB;
                cmdOLEDB.CommandText = Request.Command;
				cmdOLEDB.CommandType = Request.CommandType;
                cmdOLEDB.CommandTimeout = Request.CmndTimeout;

				if (Request.ParamCollection.Count > 0){
					foreach(ODMRequest.Parameter oParam in Request.ParamCollection)
						if(oParam.ParameterValue == "")
							cmdOLEDB.Parameters.Add(oParam.ParameterObject);
						else
							prmOLEDB = cmdOLEDB.Parameters.Add(oParam.ParameterName, oParam.ParameterValue);
				}
				prmOLEDB = prmOLEDB; //clears a compiler warning
				daOleDb = new OleDbDataAdapter(cmdOLEDB);
				daOleDb.Fill(ODMDataSet);

				if(primaryKey != ""){
					DataColumn[] dcArray = new DataColumn[10];
					dcArray[0] = ODMDataSet.Tables[0].Columns[primaryKey];
					ODMDataSet.Tables[0].PrimaryKey = dcArray;
				}
			}
			catch(OleDbException exOLEDB) {
				Request.DbException = exOLEDB;
			}

			catch (Exception ex){
				Request.DbException = ex;
			}
			finally {
				if(conOLEDB.State == ConnectionState.Open)
					conOLEDB.Close();
                if (Request.DbException != null)
                {
                    throw new Exception("OLEDB_DM: An Error Was Encountered During a Database Operation:" + Environment.NewLine +
                        Environment.NewLine + Request.DbException.ToString());
                }
			}

			return ODMDataSet;
		}


		public ArrayList ExecuteDataReader(ODMRequest Request) {
			return ExecuteDataReader(ref Request,1);
		}		

		public ArrayList ExecuteDataReader(ref ODMRequest Request) {
			return ExecuteDataReader(ref Request,1);
		}

		public ArrayList ExecuteDataReader(ODMRequest Request, int colCount) {
			return ExecuteDataReader(ref Request,colCount);
		}

		public ArrayList ExecuteDataReader(ref ODMRequest Request,int drColCount){
			//drColCount is the number of columns returned by the DataReader (the 'dr' in drColCount)
			//as determined by the number of SELECT elements in Request.Command
			//
			//ie: 'Select value From MyTable Where id = 0' would return ONE COLUMN
			//		named 'value' - possibly WITH MULTIPLE ROWS --> drColCount = 1.
			//
			//    'Select value,type,color From MyTable Where id = 0 And index = 4' 
			//		would return THREE COLUMNS named 'value','type' and 'color' in 
			//		ONE ROW  --> drColCount = 3.
			//
			//if there's a chance that the query will return multiple rows each with
			//multiple columns, use a DataSet or a Stored Proc instead of a DataReader
			//
			//NOTE: Don't use this when calling a Stored Procedure - the error you get is:
			//'Wrong number or type of parameters...'

			if(LogEnabled)
				LogRequestParams(Request) ;
			conOLEDB = new OleDbConnection();
			OleDbCommand cmdOLEDB = new OleDbCommand();
			OleDbDataReader drOLEDB  = null;
			ArrayList dbValues = new ArrayList();			
			int itemIndx = 0;

			try{
				conOLEDB.ConnectionString = Request.ConnectString;
				conOLEDB.Open();
				cmdOLEDB.Connection = conOLEDB;
				cmdOLEDB.CommandText = Request.Command;
				cmdOLEDB.CommandType = Request.CommandType;

				drOLEDB = cmdOLEDB.ExecuteReader();		       
			}
			catch (Exception ex){
				Debug.WriteLine(ex.Message);
				Request.DbException = ex;
			}
			finally {
                if (Request.DbException != null)
                {
                    throw new Exception("OLEDB_DM: An Error Was Encountered During a Database Operation:" + Environment.NewLine +
                        Environment.NewLine + Request.DbException.ToString());
                }
                else
                {
                    if (drOLEDB.HasRows)
                    {
                        while (drOLEDB.Read())
                        {
                            //read each row returned
                            for (itemIndx = 0; itemIndx < drColCount; itemIndx++) //read each column of each row
                                dbValues.Add(drOLEDB[itemIndx]);
                        }
                    }
                    else
                    {
                        while (drOLEDB.Read())
                            dbValues.Add(drOLEDB.GetValue(0));
                    }
                }
			    if(conOLEDB.State == ConnectionState.Open)
					conOLEDB.Close();
			}
			return dbValues;
		}

		
		public ArrayList ExecuteNonQueryOutParams(ODMRequest Request) {
			return ExecuteNonQueryOutParams(ref Request);
		}

		public ArrayList ExecuteNonQueryOutParams(ref ODMRequest Request){
			//works. returns output and input/output params			
			if(LogEnabled)
				LogRequestParams(Request) ;
			conOLEDB = new OleDbConnection();
			OleDbCommand cmdOLEDB  = new OleDbCommand();
			ArrayList oParam = new ArrayList(Request.ParamCollection);
			ArrayList outParamValues = new ArrayList();

			try{
				conOLEDB.ConnectionString = Request.ConnectString;
				conOLEDB.Open();
				cmdOLEDB.Connection = conOLEDB;
				cmdOLEDB.CommandText = Request.Command;
				cmdOLEDB.CommandType = Request.CommandType;				
				if( Request.ParamCollection.Count > 0){					
					for(int x = 0; x < oParam.Count; x++){						
						cmdOLEDB.Parameters.Add(oParam[x]);
					}
				}				
				cmdOLEDB.ExecuteNonQuery();//<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
			}catch (OleDbException exOLEDB) {
				Request.DbException = exOLEDB;
			}catch (Exception ex){
				Request.DbException = ex;
			}finally{
				if(conOLEDB.State == ConnectionState.Open)
					conOLEDB.Close();
				if(Request.DbException != null) {	
					string objState = "";
					if(conOLEDB == null)
						objState += "conOLEDB=null  ";
					if(cmdOLEDB == null)
						objState += "cmdOLEDB=null  ";
					if(Request == null)						
						objState += "Request=null";
					throw new Exception ("An Error Was Encountered During a Database Operation:" + Environment.NewLine + 
						objState + Environment.NewLine + Request.DbException.ToString());											
				}
			}
			for(int x = 0; x < oParam.Count; x++){	
				string direction = ((OleDbParameter)oParam[x]).Direction.ToString().ToLower();
				if( direction == "output" || direction == "inputoutput")
					outParamValues.Add((OleDbParameter)oParam[x]);
			}
			return outParamValues;							
		}


		public void ExecuteNonQuery(ref ODMRequest Request){
			if(LogEnabled)
				LogRequestParams(Request) ;
			conOLEDB = new OleDbConnection();
			OleDbCommand cmdOLEDB  = new OleDbCommand();
			OleDbParameter prmOLEDB = new OleDbParameter();

			ArrayList oParam = new ArrayList(Request.ParamCollection);
			try{
				conOLEDB.ConnectionString = Request.ConnectString;
				conOLEDB.Open();
				cmdOLEDB.Connection = conOLEDB;
				cmdOLEDB.CommandText = Request.Command;
				cmdOLEDB.CommandType = Request.CommandType;

				if( Request.ParamCollection.Count > 0){
					for(int x = 0; x < oParam.Count; x++)
					{						
						cmdOLEDB.Parameters.Add(oParam[x]);
					}					
				}
				prmOLEDB = prmOLEDB; //clears a compiler warning
				cmdOLEDB.ExecuteNonQuery();
			}

			catch (OleDbException exOLEDB){
				Request.DbException = exOLEDB;
			}

			catch (Exception ex){
				Request.DbException = ex;
			}

			finally{
				if(conOLEDB.State == ConnectionState.Open)
					conOLEDB.Close();
				if(Request.DbException != null) {						
					throw new Exception("OLEDB_DM: An Error Was Encountered During a Database Operation:" + Environment.NewLine + 
						Environment.NewLine + Request.DbException.ToString());							
				}
			}				
		}

		public void ExecuteDataWriter(ref ODMRequest Request){
			//
			//need to test this
			//
			if(LogEnabled)
				LogRequestParams(Request) ;
			conOLEDB = new OleDbConnection();
			OleDbCommand cmdOLEDB  = new OleDbCommand();
			ArrayList oParam = new ArrayList(Request.ParamCollection);

			OleDbTransaction tranOLEDB = null;

			try{
				conOLEDB.ConnectionString = Request.ConnectString;
				conOLEDB.Open();
				cmdOLEDB.Connection = conOLEDB;
				cmdOLEDB.CommandText = Request.Command;
				cmdOLEDB.CommandType = Request.CommandType;

					if( Request.ParamCollection.Count > 0){
					
					for(int x = 0; x < oParam.Count; x++){						
						cmdOLEDB.Parameters.Add(oParam[x]);
					}
				}					
				cmdOLEDB.ExecuteNonQuery();

				if(Request.Transactional)
					tranOLEDB = conOLEDB.BeginTransaction();
				
				}
			catch(OleDbException ex){						 
				Request.DbException = ex;
				if(Request.Transactional)
					tranOLEDB.Rollback();
			}
			catch(Exception ex){
				Request.DbException = ex;
				if(Request.Transactional)
					tranOLEDB.Rollback();
			}

			finally{
				if(Request.Transactional)
					tranOLEDB.Commit();

				if(conOLEDB.State == ConnectionState.Open)
					conOLEDB.Close();
                if (Request.DbException != null)
                {
                    throw new Exception("OLEDB_DM: An Error Was Encountered During a Database Operation:" + Environment.NewLine +
                        Environment.NewLine + Request.DbException.ToString());
                }
			}
		
		}

		public ArrayList ExecuteDataReaderOutParams(ref ODMRequest Request){
			//
			//need to test this
			//
			if(LogEnabled)
				LogRequestParams(Request) ;
			conOLEDB = new OleDbConnection();
			OleDbCommand cmdOLEDB = new OleDbCommand();
			OleDbDataReader drOLEDB  = null;
			ArrayList oParam = new ArrayList(Request.ParamCollection);
			ArrayList outParamValues = new ArrayList();

			try{
				conOLEDB.ConnectionString = Request.ConnectString;
				conOLEDB.Open();
				cmdOLEDB.Connection = conOLEDB;
				cmdOLEDB.CommandText = Request.Command;
				cmdOLEDB.CommandType = Request.CommandType;
				if( Request.ParamCollection.Count > 0){					
					for(int x = 0; x < oParam.Count; x++){						
						cmdOLEDB.Parameters.Add(oParam[x]);
					}
				}	

				drOLEDB = cmdOLEDB.ExecuteReader();		       
			}			
			catch (Exception ex){
				Debug.WriteLine(ex.Message);
				//MessageBox.Show(ex.Message, "Data Read Error", MessageBoxButtons.OK); 
				Request.DbException = ex;
			}
			finally {				
				if(Request.DbException != null) {						
					throw new Exception("OLEDB_DM: An Error Was Encountered During a Database Operation:" + Environment.NewLine + 
						Environment.NewLine + Request.DbException.ToString());							
				}else {
					while(drOLEDB.Read()) {
						outParamValues.Add(drOLEDB.GetValue(0));
					}
					for(int x = 0; x < oParam.Count; x++){	
						string direction = ((OleDbParameter)oParam[x]).Direction.ToString().ToLower();
						if( direction == "output" || direction == "inputoutput")
							outParamValues.Add((OleDbParameter)oParam[x]);
					}
				}
				if(conOLEDB.State == ConnectionState.Open)
					conOLEDB.Close();
			}			
			return outParamValues;
		}


		private void LogRequestParams(ODMRequest Request) {
			ArrayList paramList = new ArrayList();		
			paramList = (ArrayList)Request.ParamCollection.Clone();
			string logInfo = "Request Object Values:" + Environment.NewLine;
			logInfo += "ConnectString= " + Request.ConnectString + Environment.NewLine;
			logInfo += "Command= " + Request.Command + Environment.NewLine;
			foreach(OleDbParameter param in paramList) {
				logInfo += "Param: " + param.ParameterName.ToString() + " : " + param.Value.ToString() + Environment.NewLine;
			}
			//odmLog.Write(logInfo);
		}
	}
}
