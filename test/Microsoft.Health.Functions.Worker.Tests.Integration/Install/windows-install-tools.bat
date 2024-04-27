winget list foo > nul 2>&1

if not ERRORLEVEL 0 winget install Microsoft.Azure.FunctionsCoreTools
