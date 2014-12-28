var widgets=new Array();

var resultslist=null;
var numPerPage=10;
var numHits=0;

var request=new BrowseRequest(numPerPage);

function isNumeric(sText){
   var ValidChars = "0123456789.";
   var IsNumber=true;
   var Char;

 
   for (i = 0; i < sText.length && IsNumber == true; i++) 
      { 
      Char = sText.charAt(i); 
      if (ValidChars.indexOf(Char) == -1) 
         {
         IsNumber = false;
         }
      }
   return IsNumber;   
}

function format(val, count) {
	var parts=val.split("|");
	
	//for (var i=0;i<parts.length;++i){
//		parts[i]=Number(parts[i]);
	//}
	
	if (parts.length==2){
		if (parts[0]==parts[1]){
			return parts[0]+" ("+count+")";
		}
		else{
			return parts[0]+" - "+parts[1]+" ("+count+")";
		}
	}
	else{
		if (parts[0]==null || parts[0].length==0){
			return parts[1]+" ("+count+")";
		}
		else{
			return parts[0]+" ("+count+")";
		}
	}
}

function formatPrice(val,count){
	var parts=val.split("|");
	
	for (var i=0;i<parts.length;++i){
		parts[i]=Number(parts[i]);
	}
	if (parts.length==2){
		if (parts[0]==parts[1]){
			return "$"+parts[0]+" ("+count+")";
		}
		else{
			return "$"+parts[0]+" - $"+parts[1]+" ("+count+")";
		}
	}
	else{
		if (parts[0]==null || parts[0].length==0){
			return "< $"+parts[1]+" ("+count+")";
		}
		else{
			return "> $"+parts[0]+" ("+count+")";
		}
	}
}

/* Functions for calculating the total on the product page */
/* Source: http://javascript.internet.com/forms/currency-format.html */
function formatCurrency(num) {
    num = num.toString().replace(/\$|\,/g, '');
    if (isNaN(num))
        num = "0";
    sign = (num == (num = Math.abs(num)));
    num = Math.floor(num * 100 + 0.50000000001);
    cents = num % 100;
    num = Math.floor(num / 100).toString();
    if (cents < 10)
        cents = "0" + cents;
    for (var i = 0; i < Math.floor((num.length - (1 + i)) / 3) ; i++)
        num = num.substring(0, num.length - (4 * i + 3)) + ',' +
        num.substring(num.length - (4 * i + 3));
    return (((sign) ? '' : '-') + '$' + num + '.' + cents);
}

function formatYear(num) {
    if (isNaN(num))
        num = "00000000000000001900";
    var numString = num.toString();
    numString = numString.substring(numString.length - 4);
    return numString;
}

function formatMileage(num) {
    if (isNaN(num))
        num = "0";
    var numFloat = parseFloat(num);
    return Math.round(numFloat);
}

// 'improve' Math.round() to support a second argument
var _round = Math.round;
Math.round = function (number, decimals /* optional, default 0 */) {
    if (arguments.length == 1)
        return _round(number);

    var multiplier = Math.pow(10, decimals);
    return _round(number * multiplier) / multiplier;
}

function updateSearchStat(browseResponse){
    numHits = browseResponse.NumHits;
    
	var hitStatElem=document.getElementById("hitcount");
	var hitstat=numHits + " out of " + browseResponse.TotalDocs + " ("+browseResponse.Time+" seconds)";
	hitStatElem.innerHTML=hitstat;
}


function loadBody(){
	api.browse(request,handleResponse);
}

function reset(){
	request.reset();
	api.browse(request,handleResponse);
	var search=document.getElementById("search");
	search.value="";
}

function handleSelection(field,value){

	var selection=request.selections[field];
	selection.clear();
	if (value!=null && value.length>0){
		selection.addValue(value);
	}
	request.offset=0;
	api.browse(request,handleResponse);
}

function handleResultListChange(response){
	//var response=JSON.parse(response);
	resultslist.update(response.Hits,request.offset);
	updateSearchStat(response);
}

function handleSortChange(sortBy){
	request.toggleSort(sortBy);
	api.browse(request,handleResultListChange);
}


function handleSearch(){
    var elem = document.getElementById("search");
	request.queryString=elem.value;
	api.browse(request,handleResponse);	
}

function handlePaging(action){
	var numPages=parseInt(numHits/numPerPage);
	var remainder=parseInt(numHits%numPerPage);
	if (remainder==0 && numPages>0) numPages--;

	var whichPage=parseInt(request.offset/numPerPage);
	
	if (action=="top") {
		if (request.offset==0) return;
		request.offset=0;
	}
	else if (action=="up") {
		if (request.offset==0) return;
			request.offset-=numPerPage;
	}
	else if (action=="down"){
		if (whichPage<numPages)
			request.offset+=numPerPage;
	}
	else if (action=="bottom"){
		if (whichPage<numPages)
			request.offset=numPages*numPerPage;
	}
	else return;
	
	api.browse(request,handleResultListChange);
}

function handleRemoveTag(field,tag){
    var tagSel=request.selections[field];
	var localHash=toHash(tagSel.values);
	delete localHash[tag];
	tagSel.values=toArray(localHash);		
	api.browse(request,handleResponse);
}

function tagselected(field,tag){
    var tagSel=request.selections[field];
	var localHash=toHash(tagSel.values);
	localHash[tag]=tag;
	tagSel.values=toArray(localHash);
	api.browse(request,handleResponse);
}

function handleResponse(response) {
	// populate stats
    updateSearchStat(response);
	    
    //populate widgets
    var choices = response.Choices;
	for (var i = 0; i < widgets.length; ++i) {
		widgets[i].update(request.selections, choices);		
	}
	resultslist.update(response.Hits, request.offset);
}

