
window.initViz = function (containerDiv, url) {
    console.log("initViz called for " + url);
    if (!containerDiv) throw "no container provided";
    if (!url) throw "no url provided";

    var options = {
        hideTabs: true,
        onFirstInteractive: function () {
            console.log("Run this code when the viz has finished loading.");
        }
    };
    console.log("initViz calling .Viz()");
    var viz = new tableau.Viz(containerDiv, url, options);
}