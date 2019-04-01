using System.Transactions;

namespace R2A.ReportApi.Service.Infrastructure
{
    /**
      * Po uzoru na:
      * https://blogs.msdn.microsoft.com/dbrowne/2010/06/03/using-new-transactionscope-considered-harmful/
      * Default postavke nisu dovoljno sigurne pa je bolje koristiti postavke ispod.
      * **/


    public class TransactionUtils
    {
        public static TransactionScope CreateTransactionScope()
        {
            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = TransactionManager.MaximumTimeout
            };
            return new TransactionScope(TransactionScopeOption.Required, transactionOptions);
        }

        public static TransactionScope CreateTransactionScope(TransactionScopeOption option)
        {
            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = TransactionManager.MaximumTimeout
            };
            return new TransactionScope(option, transactionOptions);
        }

        public static TransactionScope CreateAsyncTransactionScope()
        {
            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = TransactionManager.MaximumTimeout
            };
            return new TransactionScope(TransactionScopeOption.Required, transactionOptions,
                TransactionScopeAsyncFlowOption.Enabled);
        }


        public static TransactionScope CreateAsyncTransactionScope(TransactionScopeOption option)
        {
            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = TransactionManager.MaximumTimeout
            };
            return new TransactionScope(option, transactionOptions, TransactionScopeAsyncFlowOption.Enabled);
        }
    }
}