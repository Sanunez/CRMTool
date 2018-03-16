# CRMTool Documentation

### **_Description_**:
The CRMTool is designed to support **CRUD** operations. (Create Retrieve Update Delete) These operations are dependent of a .CSV file being fed into the tool into the incoming directory. If the tool detects an issue with the format of the file it will get redirected to an Error directory, However there is only an error with a record in the file then it will record the line in the error file and continue. Once the file is finished it will be concidered as successfully processed and placed in the Done directory as well with an error file with the turbulent records.

### CRM Tool
**CSV Setup**
Every CSV file consists of the same first line that will communicate to the program what action it will be performing and what format it will read it in.

<table style="width:100%">
  <tr>
    <th>Create</th>
    <th>Update</th> 
    <th>Delete</th>
  </tr>
  <tr>
    <td>
      <table>
        <tr>
          <td>Entity</td>
          <td>Action</td>
          <td></td>
          <td></td>
        </tr>
        <tr>
          <td>Attribute1</td>
          <td>Attribute2</td>
          <td>...</td>
          <td>AttributeN</td>
        </tr>
        <tr>
          <td>Data1</td>
          <td>Data2</td>
          <td>...</td>
          <td>DataN</td>
        </tr>
      </table>
    </td>
     <td>
    <table>
        <tr>
          <td>Entity</td>
          <td>Action</td>
          <td></td>
          <td></td>
        </tr>
        <tr>
          <td>GUID</td>
          <td>Attribute1</td>
          <td>...</td>
          <td>AttributeN</td>
        </tr>
        <tr>
          <td><strong><i>Guid Data</i></strong></td>
          <td>Data1</td>
          <td>...</td>
          <td>DataN</td>
        </tr>
      </table>
  </td>
    <td>
    <table>
        <tr>
          <td>Entity</td>
          <td>Action</td>
        </tr>
        <tr>
          <td>GUID</td>
          <td></td>
        </tr>
        <tr>
          <td><strong><i>Guid Data</i></strong></td>
          <td></td>
        </tr>
      </table>
  </td>
  </tr>
 
</table>
