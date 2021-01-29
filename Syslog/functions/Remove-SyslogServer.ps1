function Remove-SyslogServer
{
<#
	.SYNOPSIS
		Stop and remove a syslog server.
	
	.DESCRIPTION
		Stop and remove a syslog server.
	
	.PARAMETER Server
		The server object to remove.
		Provide a 'Syslog.Server' object returned by Get-SyslogServer.
	
	.PARAMETER EnableException
		This parameters disables user-friendly warnings and enables the throwing of exceptions.
		This is less user friendly, but allows catching exceptions in calling scripts.
	
	.PARAMETER Confirm
		If this switch is enabled, you will be prompted for confirmation before executing any operations that change state.
	
	.PARAMETER WhatIf
		If this switch is enabled, no actions are performed but informational messages will be displayed that explain what would happen if the command were to run.
	
	.EXAMPLE
		PS C:\> Get-SyslogServer | Remove-SyslogServer
	
		Stops and removes all currently existing syslog servers.
#>
	[CmdletBinding(SupportsShouldProcess = $true)]
	Param (
		[Parameter(Mandatory = $true, ValueFromPipeline = $true)]
		[Syslog.Server[]]
		$Server,
		
		[switch]
		$EnableException
	)
	
	process
	{
		foreach ($serverObject in $Server) {
			if ($serverObject.State -eq 'Running') {
				Invoke-PSFProtectedCommand -Action "Stopping Server from $($serverObject.ListenOn):$($serverObject.InPort) to $($serverObject.OutServer):$($serverObject.OutPort)" -ScriptBlock {
					$serverObject.Stop()
				} -Target $serverObject -EnableException $EnableException -PSCmdlet $PSCmdlet -Continue
			}
			Invoke-PSFProtectedCommand -Action "Removing Server from $($serverObject.ListenOn):$($serverObject.InPort) to $($serverObject.OutServer):$($serverObject.OutPort)" -ScriptBlock {
				$script:servers.Remove($serverObject)
			} -Target $serverObject -EnableException $EnableException -PSCmdlet $PSCmdlet -Continue
		}
	}
}