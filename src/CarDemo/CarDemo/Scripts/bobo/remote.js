

function JSONRPC(url){
	this.url=url;
	this.sndReq=function(action,request,callback,isXML) {	
	    var data = request.toJSON();
	    //alert(JSON.stringify(data));

		$.ajax({
		    url: url,
		    data: JSON.stringify(data),
		    type: 'POST',
		    contentType: 'application/json, charset=utf-8',
		    dataType: 'json',
		    async: true,
		    processData: false,
            cache: false,
		    traditional: true,
		    success: function (data) {
		        callback(data);
		    },
		    error: function (jqXHR, textStatus, errorThrown) {
		        alert(jqXHR.responseText);
		    }
		});
	}
}

function BoboAPI(url){
	this.transport=new JSONRPC(url);
	this.browse=function(browsereq,callback){
		this.transport.sndReq("browse",browsereq,callback,false);
	}
}