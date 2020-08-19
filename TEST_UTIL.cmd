c:
cd \DATA\IVII\Soft\VS2012Projects\VSX\VStorageUtil\bin\Release

VStorageUtil.exe create  -n vxmlbase  -s 5  -e 2 -r c:\data\tt\vs1
VStorageUtil.exe create  -n vxmlcont  -s 15  -e 15 -r c:\data\tt\vs1
VStorageUtil.exe list  -n *  -r c:\data\tt\vs1
VStorageUtil.exe add  -n vxmlbase  -s 3 -r c:\data\tt\vs1
VStorageUtil.exe ext  -n vxmlcont  -s 30 -r c:\data\tt\vs1
VStorageUtil.exe restore -n * -r c:\data\tt\vs1 -d C:\DATA\TT\TEST_DATA\DUMP
VStorageUtil.exe dump -n * -r c:\data\tt\vs1 -d c:\data\tt\vs1\dump
VStorageUtil.exe remove -n * -r c:\data\tt\vs1

pause