// =============================================================
// BytesRoad.NetSuit : A free network library for .NET platform 
// =============================================================
//
// Copyright (C) 2004-2005 BytesRoad Software
// 
// Project Info: http://www.bytesroad.com/NetSuit
// 
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
// ========================================================================== 
// Changed by: NRPG
using System.Diagnostics;

namespace BytesRoad.Diag
{
    /// <summary>
    /// Provides a multilevel switch to control 
    /// tracing output of the BytesRoad.NetSuit Library.
    /// </summary>
    /// <remarks>
    /// By using <b>NSTraceOptions</b> class you may setup
    /// level of the tracing information output for the 
    /// BytesRoad.NetSuit Library, form appropriate set of listeners as
    /// well as to set up other tracing options.
    /// <para>
    /// There is four different level of tracing you may set via 
    /// <see cref="BytesRoad.Diag.NSTraceOptions.Level">Level</see>
    /// property. You need to use 
    /// <see cref="BytesRoad.Diag.NSTraceOptions.Listeners"/>
    /// property to configure set of listeners which you want to
    /// be subscribed for tracing information notifications of 
    /// BytesRoad.NetSuit Library. One of the simple way 
    /// to enable tracing is to use 
    /// <see cref="System.Diagnostics.TextWriterTraceListener"/>
    /// class provided by .NET Framework (see example below).
    /// </para>
    /// 
    /// <code>
    /// using System;
    /// using System.Diagnostics;
    /// using System.IO;
    /// 
    /// using BytesRoad.Diag;
    /// 
    /// // Setup tracing options - redirect all tracing
    /// // into the 'c:\temp\NetSuitTrace.txt' file
    /// void SetupNetSuitTracing()
    /// {
    ///        //trace only errors
    ///        NSTraceOptions.Level = TraceLevel.Error;
    /// 
    ///        //create instance of listener, provided by the .net framework
    ///        TextWriterTraceListener listener =
    ///            new TextWriterTraceListener(@"c:\temp\NetSuitTrace.txt");
    /// 
    ///        //adding listener to the collection of listeners
    ///        NSTraceOptions.Listeners.Add(listener);
    /// 
    ///        //disable caching
    ///        NSTraceOptions.AutoFlush = true;
    /// }
    /// </code>
    /// </remarks>
    public class NSTraceOptions
    {
        static bool _useSystemTrace = false;
        static NSTraceListeners _listeners = new NSTraceListeners();
        static TraceLevel _level = TraceLevel.Off;
        static bool _autoFlush = false;
        
        private NSTraceOptions()
        {
        }

        #region Attributes

        /// <summary>
        /// Gets a value indicating whether the 
        /// <see cref="BytesRoad.Diag.NSTraceOptions.Level">Level</see>
        /// is set to <b>Error</b>, <b>Warning</b>, 
        /// <b>Info</b>, or <b>Verbose</b>.
        /// </summary>
        /// <value>
        /// <b>true</b> if the 
        /// <see cref="BytesRoad.Diag.NSTraceOptions.Level">Level</see>
        /// is set to <b>Error</b>, <b>Warning</b>, 
        /// <b>Info</b>, or <b>Verbose</b>; otherwise, <b>false</b>.
        /// </value>
        static public bool TraceError
        {
            get
            {
                return (_level == TraceLevel.Error) || 
                    (_level == TraceLevel.Warning) ||
                    (_level == TraceLevel.Info) ||
                    (_level == TraceLevel.Verbose);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the 
        /// <see cref="BytesRoad.Diag.NSTraceOptions.Level">Level</see>
        /// is set to <b>Warning</b>, 
        /// <b>Info</b>, or <b>Verbose</b>.
        /// </summary>
        /// <value>
        /// <b>true</b> if the 
        /// <see cref="BytesRoad.Diag.NSTraceOptions.Level">Level</see>
        /// is set to <b>Warning</b>, 
        /// <b>Info</b>, or <b>Verbose</b>; otherwise, <b>false</b>.
        /// </value>
        static public bool TraceWarning
        {
            get
            {
                return (_level == TraceLevel.Warning) ||
                    (_level == TraceLevel.Info) ||
                    (_level == TraceLevel.Verbose);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the 
        /// <see cref="BytesRoad.Diag.NSTraceOptions.Level">Level</see>
        /// is set to <b>Info</b> or <b>Verbose</b>.
        /// </summary>
        /// <value>
        /// <b>true</b> if the 
        /// <see cref="BytesRoad.Diag.NSTraceOptions.Level">Level</see>
        /// is set to <b>Info</b> or <b>Verbose</b>; otherwise, <b>false</b>.
        /// </value>
        static public bool TraceInfo
        {
            get
            {
                return     (_level == TraceLevel.Info) ||
                    (_level == TraceLevel.Verbose);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the 
        /// <see cref="BytesRoad.Diag.NSTraceOptions.Level">Level</see>
        /// is set to <b>Verbose</b>.
        /// </summary>
        /// <value>
        /// <b>true</b> if the 
        /// <see cref="BytesRoad.Diag.NSTraceOptions.Level">Level</see>
        /// is set to <b>Verbose</b>; otherwise, <b>false</b>.
        /// </value>
        static public bool TraceVerbose
        {
            get
            {
                return     (_level == TraceLevel.Verbose);
            }
        }

        /// <summary>
        /// Gets or sets the trace level that specifies 
        /// the messages to output for tracing.
        /// </summary>
        /// <value>
        /// One of the 
        /// <see cref="System.Diagnostics.TraceLevel">TraceLevel</see> 
        /// values that specify the messages to output for tracing.
        /// </value>
        static public TraceLevel Level
        {
            get { return _level; }
            set { _level = value; }
        }

        /// <summary>
        /// Gets or sets the value indicating whether all tracing
        /// output will be duplicated into the system tracer.
        /// </summary>
        /// <value>
        /// <b>true</b> if all tracing output duplicated into the system 
        /// tracer;
        /// <b>false</b>, otherwise. Default value is <b>false</b>.
        /// </value>
        /// <remarks>
        /// By default all tracing output produced by the 
        /// BytesRoad.NetSuit Library goes to the listeners
        /// which can be found in the 
        /// <see cref="BytesRoad.Diag.NSTraceListeners">NSTraceListeners</see> 
        /// collection. By setting <b>UseSystemTrace</b> property to 
        /// <b>true</b> all tracing output will be duplicated into the
        /// system tracer. It is achieved by coping all output to the 
        /// <see cref="System.Diagnostics.Trace">Trace</see> 
        /// class.
        /// </remarks>
        static public bool UseSystemTrace
        {
            get { return _useSystemTrace; }
            set { _useSystemTrace = value; }
        }

        /// <summary>
        /// Gets the collection of listeners that 
        /// is monitoring the trace output.
        /// </summary>
        /// <value>
        /// A 
        /// <see cref="BytesRoad.Diag.NSTraceListeners">NSTraceListeners</see> 
        /// that represents a collection of type 
        /// <see cref="System.Diagnostics.TraceListener">TraceListener</see> 
        /// monitoring the trace output.
        /// </value>
        static public NSTraceListeners Listeners
        {
            get { return _listeners; }
        }

        /// <summary>
        /// Gets or sets whether 
        /// <see cref="System.Diagnostics.TraceListener.Flush">Flush</see> 
        /// should be called on the 
        /// <see cref="BytesRoad.Diag.NSTraceListeners">Listeners</see> 
        /// after every write.
        /// </summary>
        /// <value>
        /// <b>true</b> if 
        /// <see cref="System.Diagnostics.TraceListener.Flush">Flush</see> 
        /// is called on the 
        /// <see cref="BytesRoad.Diag.NSTraceListeners">Listeners</see> 
        /// after every write; otherwise, <b>false</b>.
        /// </value>
        static public bool AutoFlush
        {
            get { return _autoFlush; }
            set { _autoFlush = value; }
        }
        #endregion
    }
}
