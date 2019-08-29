using System;
using System.Collections;
using System.Data;

namespace OleDBDataManager
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class ODMRequest
	{
		public ODMRequest()
		{}
		private CommandType cmndType ;
		private string cmndLiteral;
		private ArrayList paramCollection = new ArrayList();
		private Exception dbException;
		private string connectString;
		private bool transactional = false;
        private int cmndTimeOut = 30;


		public CommandType CommandType {
			get { return cmndType; }
			set { cmndType = value; }
		}

		public string Command {
			get { return cmndLiteral; }
			set { cmndLiteral = value; }
		}

		public ArrayList ParamCollection {
			get { return paramCollection; }
			set { paramCollection = value; }
		}

		public Exception DbException {
			get { return dbException; }
			set { dbException = value; }
		}

		public string ConnectString {
			get { return connectString; }
			set { connectString = value; }
		}

        public int CmndTimeout
        {
            get { return cmndTimeOut; }
            set { cmndTimeOut = value; }
        }

		public bool Transactional {
			get { return transactional; }
			set { transactional = value; }
		}

		public class Parameter {
			private string parameterName;
			private string parameterValue;
			private object parameterObject;

			public Parameter(){}

			public string ParameterName {
				get { return parameterName; }
				set { parameterName = value; }
			}

			public string ParameterValue {
				get { return parameterValue; }
				set { parameterValue = value; }
			}

			public object ParameterObject {
				get { return parameterObject; }
				set { parameterObject = value; }
			} 

		}//Class Parameter

	}
}
