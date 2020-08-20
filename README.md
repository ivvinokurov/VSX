# VSX
Virtual Storage + Virtual XML

Virtual Storage is a core platform that provides capabilities to create the customized local application data storage.
It is not a kind of relational or hierarchical database but platform that includes the basic tool to create random or sequential data access mechanism.

VXML (Virtual XML) is XML-based database built on Virtual Storage platform as VStorage wrapper.
It inherits all existing Virtual Storage capabilities, such as distributed allocation of the database files, multi-space approach for specific data pools, dump/restore functions, static and dynamic storage extension, transactions, physical data consistency, etc.
VXML database is NOT a single XML document. It provides the embedded catalog structure that can be managed by user in the specific manner, XML documents are attached to catalog nodes. XQL queries may retrieve XML nodes from the specific document/element or documents list from the catalog structure using internal documents nodes as well as document names in the queries.
