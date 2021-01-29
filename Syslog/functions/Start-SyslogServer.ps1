function Start-SyslogServer
{
<#
	.SYNOPSIS
		Start a syslog server that is not yet currently running.
	
	.DESCRIPTION
		Start a syslog server that is not yet currently running.
		This operation will fail if the port it listens on is already in use.
	
	.PARAMETER Server
		The server object to start.
		Provide a 'Syslog.Server' object returned by Get-SyslogServer.
	
	.PARAMETER EnableException
		This parameters disables user-friendly warnings and enables the throwing of exceptions.
		This is less user friendly, but allows catching exceptions in calling scripts.
	
	.PARAMETER Confirm
		If this switch is enabled, you will be prompted for confirmation before executing any operations that change state.
	
	.PARAMETER WhatIf
		If this switch is enabled, no actions are performed but informational messages will be displayed that explain what would happen if the command were to run.
	
	.EXAMPLE
		PS C:\> Start-SyslogServer -Server $Server
	
		Start the server stored in $Server.
#>
	[CmdletBinding(SupportsShouldProcess = $true)]
	param (
		[Parameter(Mandatory = $true, ValueFromPipeline = $true)]
		[Syslog.Server[]]
		$Server,
		
		[switch]
		$EnableException
	)
	
	process
	{
		foreach ($serverObject in $Server) {
			Invoke-PSFProtectedCommand -Action "Starting Server from $($serverObject.ListenOn):$($serverObject.InPort) to $($serverObject.OutServer):$($serverObject.OutPort)" -ScriptBlock {
				$serverObject.Start()
			} -Target $serverObject -EnableException $EnableException -PSCmdlet $PSCmdlet -Continue
		}
	}
}