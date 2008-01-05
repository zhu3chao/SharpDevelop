﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <owner name="David Srbecký" email="dsrbecky@gmail.com"/>
//     <version>$Revision$</version>
// </file>
#region License
//  
//  Copyright (c) 2007, ic#code
//  
//  All rights reserved.
//  
//  Redistribution  and  use  in  source  and  binary  forms,  with  or without
//  modification, are permitted provided that the following conditions are met:
//  
//  1. Redistributions  of  source code must retain the above copyright notice,
//     this list of conditions and the following disclaimer.
//  
//  2. Redistributions  in  binary  form  must  reproduce  the  above copyright
//     notice,  this  list  of  conditions  and the following disclaimer in the
//     documentation and/or other materials provided with the distribution.
//  
//  3. Neither the name of the ic#code nor the names of its contributors may be
//     used  to  endorse or promote products derived from this software without
//     specific prior written permission.
//  
//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
//  AND  ANY  EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
//  IMPLIED  WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
//  ARE  DISCLAIMED.   IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
//  LIABLE  FOR  ANY  DIRECT,  INDIRECT,  INCIDENTAL,  SPECIAL,  EXEMPLARY,  OR
//  CONSEQUENTIAL  DAMAGES  (INCLUDING,  BUT  NOT  LIMITED  TO,  PROCUREMENT OF
//  SUBSTITUTE  GOODS  OR  SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
//  INTERRUPTION)  HOWEVER  CAUSED  AND  ON ANY THEORY OF LIABILITY, WHETHER IN
//  CONTRACT,  STRICT  LIABILITY,  OR  TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
//  ARISING  IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
//  POSSIBILITY OF SUCH DAMAGE.
//  
#endregion

using System;
using System.Diagnostics;
using System.Windows.Forms;

using Debugger;
using Debugger.AddIn.TreeModel;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Project;
using ICSharpCode.SharpDevelop.Debugging;
using BM = ICSharpCode.SharpDevelop.Bookmarks;

namespace ICSharpCode.SharpDevelop.Services
{	
	public class WindowsDebugger : IDebugger
	{
		bool useRemotingForThreadInterop = false;
		
		NDebugger debugger;
		
		Properties properties;
		
		Debugger.Process debuggedProcess;
		
		public event EventHandler<ProcessEventArgs> ProcessSelected;
		
		public NDebugger DebuggerCore {
			get {
				return debugger;
			}
		}
		
		public Debugger.Process DebuggedProcess {
			get {
				return debuggedProcess;
			}
		}
		
		protected virtual void OnProcessSelected(ProcessEventArgs e)
		{
			if (ProcessSelected != null) {
				ProcessSelected(this, e);
			}
		}
		
		public Properties Properties {
			get {
				return properties;
			}
		}
		
		public bool ServiceInitialized {
			get {
				return debugger != null;
			}
		}
		
		public WindowsDebugger()
		{
			properties = PropertyService.Get("DebuggerProperties", new Properties());
		}
		
		#region IDebugger Members
		
		string errorDebugging      = "${res:XML.MainMenu.DebugMenu.Error.Debugging}";
		string errorNotDebugging   = "${res:XML.MainMenu.DebugMenu.Error.NotDebugging}";
		string errorProcessRunning = "${res:XML.MainMenu.DebugMenu.Error.ProcessRunning}";
		string errorProcessPaused  = "${res:XML.MainMenu.DebugMenu.Error.ProcessPaused}";
		string errorCannotStepNoActiveFunction = "${res:MainWindow.Windows.Debug.Threads.CannotStepNoActiveFunction}";
		
		public bool IsDebugging { 
			get { 
				return ServiceInitialized && debuggedProcess != null;
			} 
		}
		
		public bool IsProcessRunning { 
			get { 
				return IsDebugging && debuggedProcess.IsRunning;
			} 
		}
		
		public bool CanDebug(IProject project)
		{
			return true;
		}
		
		public void Start(ProcessStartInfo processStartInfo)
		{
			if (IsDebugging) {
				MessageService.ShowMessage(errorDebugging);
				return;
			}
			if (!ServiceInitialized) {
				InitializeService();
			}
			string version = debugger.GetProgramVersion(processStartInfo.FileName);
			if (version.StartsWith("v1.0")) {
				MessageService.ShowMessage("${res:XML.MainMenu.DebugMenu.Error.Net10NotSupported}");
			} else if (version == null || version.Length == 0) {
				MessageService.ShowMessage("${res:XML.MainMenu.DebugMenu.Error.BadAssembly}");
			} else {
				if (DebugStarting != null)
					DebugStarting(this, EventArgs.Empty);
				
				Debugger.Process process = debugger.Start(processStartInfo.FileName,
				                                          processStartInfo.WorkingDirectory,
				                                          processStartInfo.Arguments);
				SelectProcess(process);
			}
		}
		
		public void StartWithoutDebugging(ProcessStartInfo processStartInfo)
		{
			System.Diagnostics.Process.Start(processStartInfo);
		}
		
		public void Stop()
		{
			if (!IsDebugging) {
				MessageService.ShowMessage(errorNotDebugging, "${res:XML.MainMenu.DebugMenu.Stop}");
				return;
			}
			debuggedProcess.Terminate();
		}
		
		// ExecutionControl:
		
		public void Break()
		{
			if (!IsDebugging) {
				MessageService.ShowMessage(errorNotDebugging, "${res:XML.MainMenu.DebugMenu.Break}");
				return;
			}
			if (!IsProcessRunning) {
				MessageService.ShowMessage(errorProcessPaused, "${res:XML.MainMenu.DebugMenu.Break}");
				return;
			}
			debuggedProcess.Break();
		}
		
		public void Continue()
		{
			if (!IsDebugging) {
				MessageService.ShowMessage(errorNotDebugging, "${res:XML.MainMenu.DebugMenu.Continue}");
				return;
			}
			if (IsProcessRunning) {
				MessageService.ShowMessage(errorProcessRunning, "${res:XML.MainMenu.DebugMenu.Continue}");
				return;
			}
			debuggedProcess.Continue();
		}
		
		// Stepping:
		
		public void StepInto()
		{
			if (!IsDebugging) {
				MessageService.ShowMessage(errorNotDebugging, "${res:XML.MainMenu.DebugMenu.StepInto}");
				return;
			}
			if (debuggedProcess.SelectedStackFrame == null || debuggedProcess.IsRunning) {
				MessageService.ShowMessage(errorCannotStepNoActiveFunction, "${res:XML.MainMenu.DebugMenu.StepInto}");
			} else {
				debuggedProcess.StepInto();
			}
		}
		
		public void StepOver()
		{
			if (!IsDebugging) {
				MessageService.ShowMessage(errorNotDebugging, "${res:XML.MainMenu.DebugMenu.StepOver}");
				return;
			}
			if (debuggedProcess.SelectedStackFrame == null || debuggedProcess.IsRunning) {
				MessageService.ShowMessage(errorCannotStepNoActiveFunction, "${res:XML.MainMenu.DebugMenu.StepOver}");
			} else {
				debuggedProcess.StepOver();
			}
		}
		
		public void StepOut()
		{
			if (!IsDebugging) {
				MessageService.ShowMessage(errorNotDebugging, "${res:XML.MainMenu.DebugMenu.StepOut}");
				return;
			}
			if (debuggedProcess.SelectedStackFrame == null || debuggedProcess.IsRunning) {
				MessageService.ShowMessage(errorCannotStepNoActiveFunction, "${res:XML.MainMenu.DebugMenu.StepOut}");
			} else {
				debuggedProcess.StepOut();
			}
		}
		
		public event EventHandler DebugStarting;
		public event EventHandler DebugStarted;
		public event EventHandler DebugStopped;
		public event EventHandler IsProcessRunningChanged;
		
		protected virtual void OnIsProcessRunningChanged(EventArgs e)
		{
			if (IsProcessRunningChanged != null) {
				IsProcessRunningChanged(this, e);
			}
		}
		
		/// <summary>
		/// Gets variable of given name.
		/// Returns null if unsuccessful.
		/// </summary>
		public Expression GetExpressionFromName(string variableName)
		{
			if (debuggedProcess == null || debuggedProcess.IsRunning) { 
				return null;
			} else {
				return new Expression(variableName);
			}
		}
		
		
		/// <summary>
		/// Gets the current value of the variable as string that can be displayed in tooltips.
		/// Returns null if unsuccessful.
		/// </summary>
		public string GetValueAsString(string variableName)
		{
			Expression expression = GetExpressionFromName(variableName);
			
			if (expression == null) {
				return null;
			} else {
				return expression.Evaluate(this.DebuggedProcess.SelectedStackFrame).AsString;
			}
		}
		
		/// <summary>
		/// Gets the tooltip control that shows the value of given variable.
		/// Return null if no tooltip is available.
		/// </summary>
		public DebuggerGridControl GetTooltipControl(string variableName)
		{
			Expression expression = GetExpressionFromName(variableName);
			
			if (expression == null) {
				return null;
			} else {
				return new DebuggerGridControl(new DynamicTreeDebuggerRow(new ExpressionNode(expression)));
			}
		}
		
		public bool CanSetInstructionPointer(string filename, int line, int column)
		{
			if (debuggedProcess != null && debuggedProcess.IsPaused && debuggedProcess.SelectedStackFrame != null) {
				SourcecodeSegment seg = debuggedProcess.SelectedStackFrame.CanSetIP(filename, line, column);
				return seg != null;
			} else {
				return false;
			}
		}
		
		public bool SetInstructionPointer(string filename, int line, int column)
		{
			if (CanSetInstructionPointer(filename, line, column)) {
				SourcecodeSegment seg = debuggedProcess.SelectedStackFrame.SetIP(filename, line, column);
				return seg != null;
			} else {
				return false;
			}
		}
		
		public void Dispose() 
		{
			Stop();
		}
		
		#endregion
		
		public event System.EventHandler Initialize;
		
		public void InitializeService()
		{
			if (useRemotingForThreadInterop) {
				// This needs to be called before instance of NDebugger is created
				string path = RemotingConfigurationHelpper.GetLoadedAssemblyPath("Debugger.Core.dll");
				new RemotingConfigurationHelpper(path).Configure();
			}
			
			debugger = new NDebugger();
			
			debugger.DebuggerTraceMessage    += debugger_TraceMessage;
			debugger.ProcessStarted          += debugger_ProcessStarted;
			debugger.ProcessExited           += debugger_ProcessExited;
			
			DebuggerService.BreakPointAdded  += delegate (object sender, BreakpointBookmarkEventArgs e) {
				AddBreakpoint(e.BreakpointBookmark);
			};
			
			foreach (BreakpointBookmark b in DebuggerService.Breakpoints) {
				AddBreakpoint(b);
			}
			
			if (Initialize != null) {
				Initialize(this, null);  
			}
		}
		
		void AddBreakpoint(BreakpointBookmark bookmark)
		{
			SourcecodeSegment seg = new SourcecodeSegment(bookmark.FileName, bookmark.LineNumber + 1); 
			Breakpoint breakpoint = debugger.AddBreakpoint(seg, bookmark.IsEnabled);
			MethodInvoker setBookmarkColor = delegate {
				bookmark.WillBeHit  = breakpoint.HadBeenSet || debugger.Processes.Count == 0;
			};
			breakpoint.Changed += delegate { setBookmarkColor(); };
			debugger.ProcessStarted += delegate {
				setBookmarkColor();
				// User can change line number by inserting or deleting lines
				breakpoint.SourcecodeSegment.StartLine = bookmark.LineNumber + 1;
			};
			debugger.ProcessExited  += delegate { setBookmarkColor(); };
			setBookmarkColor();
			
			BM.BookmarkManager.Removed += delegate (object sender, BM.BookmarkEventArgs e) {
				if (bookmark == e.Bookmark) {
					debugger.RemoveBreakpoint(breakpoint);
				}
			};
			bookmark.IsEnabledChanged += delegate {
				breakpoint.Enabled = bookmark.IsEnabled;
			};
		}
		
		void LogMessage(object sender, MessageEventArgs e)
		{
			DebuggerService.PrintDebugMessage(e.Message);
		}
		
		void debugger_TraceMessage(object sender, MessageEventArgs e)
		{
			LoggingService.Debug("Debugger: " + e.Message);
		}
		
		void debugger_ProcessStarted(object sender, ProcessEventArgs e)
		{
			if (debugger.Processes.Count == 1) {
				if (DebugStarted != null) {
					DebugStarted(this, EventArgs.Empty);
				}
			}
			e.Process.LogMessage += LogMessage;
		}
		
		void debugger_ProcessExited(object sender, ProcessEventArgs e)
		{
			if (debugger.Processes.Count == 0) {
				if (DebugStopped != null) {
					DebugStopped(this, e);
				}
				SelectProcess(null);
			} else {
				SelectProcess(debugger.Processes[0]);
			}
		}
		
		public void SelectProcess(Debugger.Process process)
		{
			if (debuggedProcess != null) {
				debuggedProcess.DebuggingPaused         -= debuggedProcess_DebuggingPaused;
				debuggedProcess.ExceptionThrown         -= debuggedProcess_ExceptionThrown;
				debuggedProcess.DebuggeeStateChanged    -= debuggedProcess_DebuggeeStateChanged;
				debuggedProcess.DebuggingResumed        -= debuggedProcess_DebuggingResumed;
			}
			debuggedProcess = process;
			if (debuggedProcess != null) {
				debuggedProcess.DebuggingPaused         += debuggedProcess_DebuggingPaused;
				debuggedProcess.ExceptionThrown         += debuggedProcess_ExceptionThrown;
				debuggedProcess.DebuggeeStateChanged    += debuggedProcess_DebuggeeStateChanged;
				debuggedProcess.DebuggingResumed        += debuggedProcess_DebuggingResumed;
			}
			JumpToCurrentLine();
			OnProcessSelected(new ProcessEventArgs(process));
		}
		
		void debuggedProcess_DebuggingPaused(object sender, ProcessEventArgs e)
		{
			OnIsProcessRunningChanged(EventArgs.Empty);
		}
		
		void debuggedProcess_ExceptionThrown(object sender, ExceptionEventArgs e)
		{
			if (e.Exception.ExceptionType != ExceptionType.DEBUG_EXCEPTION_UNHANDLED) {
				// Ignore the exception
				e.Continue = true;
				return;
			}
			
			JumpToCurrentLine();
			
			Debugger.Process process = this.DebuggedProcess;
			
			ExceptionForm.Result result = ExceptionForm.Show(e.Exception);
			
			// If the process was killed while the exception form was shown
			if (process.HasExpired) return;
			
			switch (result) {
				case ExceptionForm.Result.Break: 
					break;
				case ExceptionForm.Result.Continue:
					e.Continue = true;
					return;
				case ExceptionForm.Result.Ignore:
					if (e.Process.SelectedThread.InterceptCurrentException()) {
						e.Continue = true; // HACK: Start interception
					} else {
						MessageService.ShowMessage("${res:MainWindow.Windows.Debug.ExceptionForm.Error.CannotInterceptException}", "${res:MainWindow.Windows.Debug.ExceptionForm.Title}");
					}
					break;
			}
		}
		
		void debuggedProcess_DebuggeeStateChanged(object sender, ProcessEventArgs e)
		{
			JumpToCurrentLine();
		}
		
		void debuggedProcess_DebuggingResumed(object sender, ProcessEventArgs e)
		{
			if (!e.Process.Evaluating) {
				DebuggerService.RemoveCurrentLineMarker();
			}
		}
		
		public void JumpToCurrentLine()
		{
			WorkbenchSingleton.MainForm.Activate();
			DebuggerService.RemoveCurrentLineMarker();
			if (debuggedProcess != null) {
				SourcecodeSegment nextStatement = debuggedProcess.NextStatement;
				if (nextStatement != null) {
					DebuggerService.JumpToCurrentLine(nextStatement.SourceFullFilename, nextStatement.StartLine, nextStatement.StartColumn, nextStatement.EndLine, nextStatement.EndColumn);
				}
			}
		}
		
		public static void DoInPausedState(MethodInvoker action)
		{
			Debugger.Process process = ((WindowsDebugger)DebuggerService.CurrentDebugger).DebuggedProcess;
			if (process.IsPaused) {
				action();
			} else {
				EventHandler<ProcessEventArgs> onDebuggingPaused = null;
				onDebuggingPaused = delegate {
					action();
					process.DebuggingPaused -= onDebuggingPaused;
				};
				process.DebuggingPaused += onDebuggingPaused;
			}
		}
	}
}
