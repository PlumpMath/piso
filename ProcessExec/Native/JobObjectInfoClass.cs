﻿namespace ProcessExec.Native
{
    internal partial class NativeMethods
    {
        /// <summary>
        /// Used for calling the Win API
        /// </summary>
        /// <reference>http://msdn.microsoft.com/en-us/library/windows/desktop/ms686216(v=vs.85).aspx</reference>
        internal enum JobObjectInfoClass
        {
            /// <summary>
            /// The lpJobObjectInfo parameter is a pointer to a JOBOBJECT_BASIC_ACCOUNTING_INFORMATION structure.
            /// </summary>
            JobObjectBasicAccountingInformation = 1,

            /// <summary>
            /// The lpJobObjectInfo parameter is a pointer to a JOBOBJECT_BASIC_LIMIT_INFORMATION structure.
            /// </summary>
            JobObjectBasicLimitInformation = 2,

            /// <summary>
            /// The lpJobObjectInfo parameter is a pointer to a JOBOBJECT_BASIC_PROCESS_ID_LIST structure.
            /// </summary>
            JobObjectBasicProcessIdList = 3,

            /// <summary>
            /// The lpJobObjectInfo parameter is a pointer to a JOBOBJECT_BASIC_UI_RESTRICTIONS structure.
            /// </summary>
            JobObjectBasicUIRestrictions = 4,

            /// <summary>
            /// The lpJobObjectInfo parameter is a pointer to a JOBOBJECT_END_OF_JOB_TIME_INFORMATION structure.
            /// </summary>
            JobObjectEndOfJobTimeInformation = 6,

            /// <summary>
            /// The lpJobObjectInfo parameter is a pointer to a JOBOBJECT_ASSOCIATE_COMPLETION_PORT structure.
            /// </summary>
            JobObjectAssociateCompletionPortInformation = 7,

            /// <summary>
            /// The lpJobObjectInfo parameter is a pointer to a JOBOBJECT_BASIC_AND_IO_ACCOUNTING_INFORMATION structure.
            /// </summary>
            JobObjectBasicAndIoAccountingInformation = 8,

            /// <summary>
            /// The lpJobObjectInfo parameter is a pointer to a JOBOBJECT_EXTENDED_LIMIT_INFORMATION structure.
            /// </summary>
            JobObjectExtendedLimitInformation = 9,

            /// <summary>
            /// The lpJobObjectInfo parameter is a pointer to a JOBOBJECT_CPU_RATE_CONTROL_INFORMATION structure.
            /// </summary>
            /// <remarks>2008/2012+ ONLY</remarks>
            JobObjectCpuRateControlInformation = 15,

            /// <summary>
            /// The lpJobObjectInfo parameter is a pointer to a JOBOBJECT_NOTIFICATION_LIMIT_INFORMATION structure.
            /// </summary>
            /// <remarks>2008/2012+ ONLY</remarks>
            JobObjectNotificationLimitInformation = 12
        }
    }
}
