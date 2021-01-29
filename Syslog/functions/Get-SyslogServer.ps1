function Get-SyslogServer
{
<#
	.SYNOPSIS
		Returns currently prepared syslog server.
	
	.DESCRIPTION
		Returns currently prepared syslog server.
		To create a new syslog server, use New-SyslogServer.
	
	.PARAMETER OutServer
		Filter by the target server messages are being forwarded to.
		Defaults to: '*'
	
	.PARAMETER InPort
		Filter by on which port the server listens for incoming messages.
	
	.EXAMPLE
		PS C:\> Get-SyslogServer
	
		List all currently operated syslog servers
#>
	[OutputType([Syslog.Server])]
	[CmdletBinding()]
	param (
		[string]
		$OutServer = '*',
		
		[int]
		$InPort = -1
	)
	
	process
	{
		($script:servers | Where-Object { $_.OutServer -Like $OutServer -and ($InPort -eq -1 -or $_.InPort -eq $InPort) })
	}
}