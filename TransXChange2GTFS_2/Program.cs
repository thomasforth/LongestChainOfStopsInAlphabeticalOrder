using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using System.Linq;
using CsvHelper;

namespace LongestChainOfStopsInAlphabeticalOrder
{
    class Program
    {
        static void Main(string[] args)
        {

            List<ServiceAndLongestStops> ListOfServiceAndLongestStops = new List<ServiceAndLongestStops>();

            List<string> fileEntriesList = new List<string>();
            // get every xml TransXChange file in the folder
            string basePath = @"D:\Dec_2017_traveline\";
            fileEntriesList.AddRange(Directory.GetFiles(basePath + "EA", "*.xml"));
            fileEntriesList.AddRange(Directory.GetFiles(basePath + "EM", "*.xml"));
            fileEntriesList.AddRange(Directory.GetFiles(basePath + "L", "*.xml"));
            fileEntriesList.AddRange(Directory.GetFiles(basePath + "NCSD", "*.xml"));
            fileEntriesList.AddRange(Directory.GetFiles(basePath + "NE", "*.xml"));
            fileEntriesList.AddRange(Directory.GetFiles(basePath + "NW", "*.xml"));
            fileEntriesList.AddRange(Directory.GetFiles(basePath + "S", "*.xml"));
            fileEntriesList.AddRange(Directory.GetFiles(basePath + "SE", "*.xml"));
            fileEntriesList.AddRange(Directory.GetFiles(basePath + "SW", "*.xml"));
            fileEntriesList.AddRange(Directory.GetFiles(basePath + "W", "*.xml"));
            fileEntriesList.AddRange(Directory.GetFiles(basePath + "WM", "*.xml"));
            fileEntriesList.AddRange(Directory.GetFiles(basePath + "Y", "*.xml"));

            for (int i = 0; i < fileEntriesList.Count; i++)
            {
                foreach (ServiceAndLongestStops service in getLongestListOfStopsFromTransXChangeFile(fileEntriesList[i]))
                {
                    Console.WriteLine(i + " of " + fileEntriesList.Count + " files analysed. Longest = " + service.longestListOfStops.Count + "\t" + service.serviceName);
                    ListOfServiceAndLongestStops.Add(service);
                }
            }

            // get the service with the longest list, and print it out
            ServiceAndLongestStops serviceWithMostAlphabeticalStops = ListOfServiceAndLongestStops.OrderByDescending(x => x.longestListOfStops.Count).FirstOrDefault();
            Console.WriteLine("THE LONGEST PUBLIC TRANSPORT JOURNEY IN LONDON WITH ALPHABETICAL CONSECUTIVE STOPS THAT I'VE FOUND SO FAR IS...");
            Console.WriteLine(serviceWithMostAlphabeticalStops.serviceName);
            Console.WriteLine("");
            Console.WriteLine("It stops at the following stops, in order,");
            Console.WriteLine("");
            foreach (string stopName in serviceWithMostAlphabeticalStops.longestListOfStops)
            {
                Console.WriteLine(stopName);
            }

            Console.WriteLine("Writing results to longestchainofstopsinalphabeticalorder.csv");
            TextWriter _textWriter = File.CreateText("longestchainofstopsinalphabeticalorder.csv");
            CsvWriter _csvwriter = new CsvWriter(_textWriter);
            _csvwriter.WriteRecords(ListOfServiceAndLongestStops);
            _textWriter.Dispose();
            _csvwriter.Dispose();

            Console.Read();
        }

        static List<ServiceAndLongestStops> getLongestListOfStopsFromTransXChangeFile(string filePath)
        {
            //filePath = @"D:\Dec_2017_traveline\Y\SVRYWAO033A.xml";
            string XMLAsAString = File.ReadAllText(filePath, Encoding.UTF8);
            byte[] byteArray = Encoding.UTF8.GetBytes(XMLAsAString);
            MemoryStream stream = new MemoryStream(byteArray);
            XmlSerializer serializer = new XmlSerializer(typeof(TransXChange));
            TransXChange _txObject = (TransXChange)serializer.Deserialize(stream);

            // get stop points and search for long
            TransXChangeAnnotatedStopPointRef[] stopsArray = _txObject.StopPoints;

            // populate a dictionary of stoprefs to stopnames
            Dictionary<string, string> stopRefsToStopNames = new Dictionary<string, string>();
            foreach (TransXChangeAnnotatedStopPointRef stop in stopsArray)
            {
                stopRefsToStopNames.TryAdd(stop.StopPointRef, stop.CommonName);
            }

            List<ServiceAndLongestStops> ListOfServices = new List<ServiceAndLongestStops>();

            foreach (TransXChangeJourneyPatternSection patternSection in _txObject.JourneyPatternSections)
            {
                try
                {
                    // convert JourneyPatternTimingLink to a list of stopnames
                    List<string> stopNamesInOrder = new List<string>();
                    foreach (TransXChangeJourneyPatternSectionJourneyPatternTimingLink timingLink in patternSection.JourneyPatternTimingLink)
                    {
                        if (stopNamesInOrder.LastOrDefault() != stopRefsToStopNames[timingLink.From.StopPointRef])
                        {
                            stopNamesInOrder.Add(stopRefsToStopNames[timingLink.From.StopPointRef]);
                        }
                        if (stopNamesInOrder.LastOrDefault() != stopRefsToStopNames[timingLink.To.StopPointRef])
                        {
                            stopNamesInOrder.Add(stopRefsToStopNames[timingLink.To.StopPointRef]);
                        }
                    }


                    List<string> longestListOfStops = new List<string>();
                    List<string> tempStopsList = new List<string>();
                    for (int startindex = 0; startindex < stopNamesInOrder.Count; startindex++)
                    {
                        tempStopsList.Clear();
                        tempStopsList.Add(stopNamesInOrder[startindex]);
                        for (int nextIndex = startindex + 1; nextIndex < stopNamesInOrder.Count; nextIndex++)
                        {
                            // look at the next stop
                            int alphabeticalSortResult = String.Compare(stopNamesInOrder[nextIndex - 1], stopNamesInOrder[nextIndex]);
                            if (alphabeticalSortResult < 0) // WE CAN ARGUE ABOUT WHETHER THIS SHOULD BE "<=" (REPEATED STOP NAMES AS STILL IN ALPHABETICAL ORDER?!)
                            {
                                tempStopsList.Add(stopNamesInOrder[nextIndex]);
                            }
                            else
                            {
                                if (tempStopsList.Count > longestListOfStops.Count)
                                {
                                    longestListOfStops.Clear();
                                    longestListOfStops.AddRange(tempStopsList.ToArray());
                                }
                                break;
                            }
                        }
                    }

                    stream.Close();

                    ServiceAndLongestStops service = new ServiceAndLongestStops();
                    service.longestListOfStops = longestListOfStops;
                    service.length = longestListOfStops.Count;
                    service.writeableList = string.Join(", ", longestListOfStops);
                    service.serviceName = _txObject.Services.Service.Lines.Line.LineName + ", " + _txObject.Services.Service.Description;
                    service.mode = _txObject.Services.Service.Mode;
                    service.journeyPatternCode = patternSection.id;
                    ListOfServices.Add(service);
                }
                catch
                {
                    // this is extremely rare.
                }
            }
            return ListOfServices;
        }
    }

    public class ServiceAndLongestStops
    {
        public List<string> longestListOfStops { get; set; }
        public string writeableList { get; set; }
        public int length { get; set; }
        public string serviceName { get; set; }
        public string journeyPatternCode { get; set; }
        public string mode { get; set; }
    }

    // This is auto-generated from the largest TransXChange file in the Traveline dataset.
    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://www.transxchange.org.uk/", IsNullable = false)]
    public partial class TransXChange
    {

        private TransXChangeAnnotatedStopPointRef[] stopPointsField;

        private TransXChangeRouteSection[] routeSectionsField;

        private TransXChangeRoute[] routesField;

        private TransXChangeJourneyPatternSection[] journeyPatternSectionsField;

        private TransXChangeOperators operatorsField;

        private TransXChangeServices servicesField;

        private TransXChangeVehicleJourney[] vehicleJourneysField;

        private System.DateTime creationDateTimeField;

        private System.DateTime modificationDateTimeField;

        private string modificationField;

        private byte revisionNumberField;

        private string fileNameField;

        private decimal schemaVersionField;

        private bool registrationDocumentField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("AnnotatedStopPointRef", IsNullable = false)]
        public TransXChangeAnnotatedStopPointRef[] StopPoints
        {
            get
            {
                return this.stopPointsField;
            }
            set
            {
                this.stopPointsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("RouteSection", IsNullable = false)]
        public TransXChangeRouteSection[] RouteSections
        {
            get
            {
                return this.routeSectionsField;
            }
            set
            {
                this.routeSectionsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("Route", IsNullable = false)]
        public TransXChangeRoute[] Routes
        {
            get
            {
                return this.routesField;
            }
            set
            {
                this.routesField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("JourneyPatternSection", IsNullable = false)]
        public TransXChangeJourneyPatternSection[] JourneyPatternSections
        {
            get
            {
                return this.journeyPatternSectionsField;
            }
            set
            {
                this.journeyPatternSectionsField = value;
            }
        }

        /// <remarks/>
        public TransXChangeOperators Operators
        {
            get
            {
                return this.operatorsField;
            }
            set
            {
                this.operatorsField = value;
            }
        }

        /// <remarks/>
        public TransXChangeServices Services
        {
            get
            {
                return this.servicesField;
            }
            set
            {
                this.servicesField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("VehicleJourney", IsNullable = false)]
        public TransXChangeVehicleJourney[] VehicleJourneys
        {
            get
            {
                return this.vehicleJourneysField;
            }
            set
            {
                this.vehicleJourneysField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public System.DateTime CreationDateTime
        {
            get
            {
                return this.creationDateTimeField;
            }
            set
            {
                this.creationDateTimeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public System.DateTime ModificationDateTime
        {
            get
            {
                return this.modificationDateTimeField;
            }
            set
            {
                this.modificationDateTimeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Modification
        {
            get
            {
                return this.modificationField;
            }
            set
            {
                this.modificationField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte RevisionNumber
        {
            get
            {
                return this.revisionNumberField;
            }
            set
            {
                this.revisionNumberField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string FileName
        {
            get
            {
                return this.fileNameField;
            }
            set
            {
                this.fileNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal SchemaVersion
        {
            get
            {
                return this.schemaVersionField;
            }
            set
            {
                this.schemaVersionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool RegistrationDocument
        {
            get
            {
                return this.registrationDocumentField;
            }
            set
            {
                this.registrationDocumentField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeAnnotatedStopPointRef
    {

        private string stopPointRefField;

        private string commonNameField;

        private string localityNameField;

        private string localityQualifierField;

        /// <remarks/>
        public string StopPointRef
        {
            get
            {
                return this.stopPointRefField;
            }
            set
            {
                this.stopPointRefField = value;
            }
        }

        /// <remarks/>
        public string CommonName
        {
            get
            {
                return this.commonNameField;
            }
            set
            {
                this.commonNameField = value;
            }
        }

        /// <remarks/>
        public string LocalityName
        {
            get
            {
                return this.localityNameField;
            }
            set
            {
                this.localityNameField = value;
            }
        }

        /// <remarks/>
        public string LocalityQualifier
        {
            get
            {
                return this.localityQualifierField;
            }
            set
            {
                this.localityQualifierField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeRouteSection
    {

        private TransXChangeRouteSectionRouteLink[] routeLinkField;

        private string idField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("RouteLink")]
        public TransXChangeRouteSectionRouteLink[] RouteLink
        {
            get
            {
                return this.routeLinkField;
            }
            set
            {
                this.routeLinkField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeRouteSectionRouteLink
    {

        private TransXChangeRouteSectionRouteLinkFrom fromField;

        private TransXChangeRouteSectionRouteLinkTO toField;

        private ushort distanceField;

        private bool distanceFieldSpecified;

        private string directionField;

        private string idField;

        /// <remarks/>
        public TransXChangeRouteSectionRouteLinkFrom From
        {
            get
            {
                return this.fromField;
            }
            set
            {
                this.fromField = value;
            }
        }

        /// <remarks/>
        public TransXChangeRouteSectionRouteLinkTO To
        {
            get
            {
                return this.toField;
            }
            set
            {
                this.toField = value;
            }
        }

        /// <remarks/>
        public ushort Distance
        {
            get
            {
                return this.distanceField;
            }
            set
            {
                this.distanceField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool DistanceSpecified
        {
            get
            {
                return this.distanceFieldSpecified;
            }
            set
            {
                this.distanceFieldSpecified = value;
            }
        }

        /// <remarks/>
        public string Direction
        {
            get
            {
                return this.directionField;
            }
            set
            {
                this.directionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeRouteSectionRouteLinkFrom
    {

        private string stopPointRefField;

        /// <remarks/>
        public string StopPointRef
        {
            get
            {
                return this.stopPointRefField;
            }
            set
            {
                this.stopPointRefField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeRouteSectionRouteLinkTO
    {

        private string stopPointRefField;

        /// <remarks/>
        public string StopPointRef
        {
            get
            {
                return this.stopPointRefField;
            }
            set
            {
                this.stopPointRefField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeRoute
    {

        private string privateCodeField;

        private string descriptionField;

        private string routeSectionRefField;

        private string idField;

        /// <remarks/>
        public string PrivateCode
        {
            get
            {
                return this.privateCodeField;
            }
            set
            {
                this.privateCodeField = value;
            }
        }

        /// <remarks/>
        public string Description
        {
            get
            {
                return this.descriptionField;
            }
            set
            {
                this.descriptionField = value;
            }
        }

        /// <remarks/>
        public string RouteSectionRef
        {
            get
            {
                return this.routeSectionRefField;
            }
            set
            {
                this.routeSectionRefField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeJourneyPatternSection
    {

        private TransXChangeJourneyPatternSectionJourneyPatternTimingLink[] journeyPatternTimingLinkField;

        private string idField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("JourneyPatternTimingLink")]
        public TransXChangeJourneyPatternSectionJourneyPatternTimingLink[] JourneyPatternTimingLink
        {
            get
            {
                return this.journeyPatternTimingLinkField;
            }
            set
            {
                this.journeyPatternTimingLinkField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeJourneyPatternSectionJourneyPatternTimingLink
    {

        private TransXChangeJourneyPatternSectionJourneyPatternTimingLinkFrom fromField;

        private TransXChangeJourneyPatternSectionJourneyPatternTimingLinkTO toField;

        private string routeLinkRefField;

        private string runTimeField;

        private string idField;

        /// <remarks/>
        public TransXChangeJourneyPatternSectionJourneyPatternTimingLinkFrom From
        {
            get
            {
                return this.fromField;
            }
            set
            {
                this.fromField = value;
            }
        }

        /// <remarks/>
        public TransXChangeJourneyPatternSectionJourneyPatternTimingLinkTO To
        {
            get
            {
                return this.toField;
            }
            set
            {
                this.toField = value;
            }
        }

        /// <remarks/>
        public string RouteLinkRef
        {
            get
            {
                return this.routeLinkRefField;
            }
            set
            {
                this.routeLinkRefField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "duration")]
        public string RunTime
        {
            get
            {
                return this.runTimeField;
            }
            set
            {
                this.runTimeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeJourneyPatternSectionJourneyPatternTimingLinkFrom
    {

        private string activityField;

        private string stopPointRefField;

        private string timingStatusField;

        private byte sequenceNumberField;

        /// <remarks/>
        public string Activity
        {
            get
            {
                return this.activityField;
            }
            set
            {
                this.activityField = value;
            }
        }

        /// <remarks/>
        public string StopPointRef
        {
            get
            {
                return this.stopPointRefField;
            }
            set
            {
                this.stopPointRefField = value;
            }
        }

        /// <remarks/>
        public string TimingStatus
        {
            get
            {
                return this.timingStatusField;
            }
            set
            {
                this.timingStatusField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte SequenceNumber
        {
            get
            {
                return this.sequenceNumberField;
            }
            set
            {
                this.sequenceNumberField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeJourneyPatternSectionJourneyPatternTimingLinkTO
    {

        private string waitTimeField;

        private string activityField;

        private string stopPointRefField;

        private string timingStatusField;

        private byte sequenceNumberField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "duration")]
        public string WaitTime
        {
            get
            {
                return this.waitTimeField;
            }
            set
            {
                this.waitTimeField = value;
            }
        }

        /// <remarks/>
        public string Activity
        {
            get
            {
                return this.activityField;
            }
            set
            {
                this.activityField = value;
            }
        }

        /// <remarks/>
        public string StopPointRef
        {
            get
            {
                return this.stopPointRefField;
            }
            set
            {
                this.stopPointRefField = value;
            }
        }

        /// <remarks/>
        public string TimingStatus
        {
            get
            {
                return this.timingStatusField;
            }
            set
            {
                this.timingStatusField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte SequenceNumber
        {
            get
            {
                return this.sequenceNumberField;
            }
            set
            {
                this.sequenceNumberField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeOperators
    {

        private TransXChangeOperatorsOperator operatorField;

        /// <remarks/>
        public TransXChangeOperatorsOperator Operator
        {
            get
            {
                return this.operatorField;
            }
            set
            {
                this.operatorField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeOperatorsOperator
    {

        private string nationalOperatorCodeField;

        private string operatorCodeField;

        private string operatorShortNameField;

        private string operatorNameOnLicenceField;

        private string tradingNameField;

        private string idField;

        /// <remarks/>
        public string NationalOperatorCode
        {
            get
            {
                return this.nationalOperatorCodeField;
            }
            set
            {
                this.nationalOperatorCodeField = value;
            }
        }

        /// <remarks/>
        public string OperatorCode
        {
            get
            {
                return this.operatorCodeField;
            }
            set
            {
                this.operatorCodeField = value;
            }
        }

        /// <remarks/>
        public string OperatorShortName
        {
            get
            {
                return this.operatorShortNameField;
            }
            set
            {
                this.operatorShortNameField = value;
            }
        }

        /// <remarks/>
        public string OperatorNameOnLicence
        {
            get
            {
                return this.operatorNameOnLicenceField;
            }
            set
            {
                this.operatorNameOnLicenceField = value;
            }
        }

        /// <remarks/>
        public string TradingName
        {
            get
            {
                return this.tradingNameField;
            }
            set
            {
                this.tradingNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeServices
    {

        private TransXChangeServicesService serviceField;

        /// <remarks/>
        public TransXChangeServicesService Service
        {
            get
            {
                return this.serviceField;
            }
            set
            {
                this.serviceField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeServicesService
    {

        private string serviceCodeField;

        private string privateCodeField;

        private TransXChangeServicesServiceLines linesField;

        private TransXChangeServicesServiceOperatingPeriod operatingPeriodField;

        private TransXChangeServicesServiceOperatingProfile operatingProfileField;

        private string registeredOperatorRefField;

        private TransXChangeServicesServiceStopRequirements stopRequirementsField;

        private string modeField;

        private string descriptionField;

        private TransXChangeServicesServiceStandardService standardServiceField;

        /// <remarks/>
        public string ServiceCode
        {
            get
            {
                return this.serviceCodeField;
            }
            set
            {
                this.serviceCodeField = value;
            }
        }

        /// <remarks/>
        public string PrivateCode
        {
            get
            {
                return this.privateCodeField;
            }
            set
            {
                this.privateCodeField = value;
            }
        }

        /// <remarks/>
        public TransXChangeServicesServiceLines Lines
        {
            get
            {
                return this.linesField;
            }
            set
            {
                this.linesField = value;
            }
        }

        /// <remarks/>
        public TransXChangeServicesServiceOperatingPeriod OperatingPeriod
        {
            get
            {
                return this.operatingPeriodField;
            }
            set
            {
                this.operatingPeriodField = value;
            }
        }

        /// <remarks/>
        public TransXChangeServicesServiceOperatingProfile OperatingProfile
        {
            get
            {
                return this.operatingProfileField;
            }
            set
            {
                this.operatingProfileField = value;
            }
        }

        /// <remarks/>
        public string RegisteredOperatorRef
        {
            get
            {
                return this.registeredOperatorRefField;
            }
            set
            {
                this.registeredOperatorRefField = value;
            }
        }

        /// <remarks/>
        public TransXChangeServicesServiceStopRequirements StopRequirements
        {
            get
            {
                return this.stopRequirementsField;
            }
            set
            {
                this.stopRequirementsField = value;
            }
        }

        /// <remarks/>
        public string Mode
        {
            get
            {
                return this.modeField;
            }
            set
            {
                this.modeField = value;
            }
        }

        /// <remarks/>
        public string Description
        {
            get
            {
                return this.descriptionField;
            }
            set
            {
                this.descriptionField = value;
            }
        }

        /// <remarks/>
        public TransXChangeServicesServiceStandardService StandardService
        {
            get
            {
                return this.standardServiceField;
            }
            set
            {
                this.standardServiceField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeServicesServiceLines
    {

        private TransXChangeServicesServiceLinesLine lineField;

        /// <remarks/>
        public TransXChangeServicesServiceLinesLine Line
        {
            get
            {
                return this.lineField;
            }
            set
            {
                this.lineField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeServicesServiceLinesLine
    {

        private string lineNameField;

        private string idField;

        /// <remarks/>
        public string LineName
        {
            get
            {
                return this.lineNameField;
            }
            set
            {
                this.lineNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeServicesServiceOperatingPeriod
    {

        private System.DateTime startDateField;

        private System.DateTime endDateField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "date")]
        public System.DateTime StartDate
        {
            get
            {
                return this.startDateField;
            }
            set
            {
                this.startDateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "date")]
        public System.DateTime EndDate
        {
            get
            {
                return this.endDateField;
            }
            set
            {
                this.endDateField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeServicesServiceOperatingProfile
    {

        private TransXChangeServicesServiceOperatingProfileRegularDayType regularDayTypeField;

        private TransXChangeServicesServiceOperatingProfileSpecialDaysOperation specialDaysOperationField;

        private TransXChangeServicesServiceOperatingProfileBankHolidayOperation bankHolidayOperationField;

        /// <remarks/>
        public TransXChangeServicesServiceOperatingProfileRegularDayType RegularDayType
        {
            get
            {
                return this.regularDayTypeField;
            }
            set
            {
                this.regularDayTypeField = value;
            }
        }

        /// <remarks/>
        public TransXChangeServicesServiceOperatingProfileSpecialDaysOperation SpecialDaysOperation
        {
            get
            {
                return this.specialDaysOperationField;
            }
            set
            {
                this.specialDaysOperationField = value;
            }
        }

        /// <remarks/>
        public TransXChangeServicesServiceOperatingProfileBankHolidayOperation BankHolidayOperation
        {
            get
            {
                return this.bankHolidayOperationField;
            }
            set
            {
                this.bankHolidayOperationField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeServicesServiceOperatingProfileRegularDayType
    {

        private TransXChangeServicesServiceOperatingProfileRegularDayTypeDaysOfWeek daysOfWeekField;

        /// <remarks/>
        public TransXChangeServicesServiceOperatingProfileRegularDayTypeDaysOfWeek DaysOfWeek
        {
            get
            {
                return this.daysOfWeekField;
            }
            set
            {
                this.daysOfWeekField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeServicesServiceOperatingProfileRegularDayTypeDaysOfWeek
    {

        private object mondayToSundayField;

        /// <remarks/>
        public object MondayToSunday
        {
            get
            {
                return this.mondayToSundayField;
            }
            set
            {
                this.mondayToSundayField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeServicesServiceOperatingProfileSpecialDaysOperation
    {

        private TransXChangeServicesServiceOperatingProfileSpecialDaysOperationDateRange[] daysOfNonOperationField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("DateRange", IsNullable = false)]
        public TransXChangeServicesServiceOperatingProfileSpecialDaysOperationDateRange[] DaysOfNonOperation
        {
            get
            {
                return this.daysOfNonOperationField;
            }
            set
            {
                this.daysOfNonOperationField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeServicesServiceOperatingProfileSpecialDaysOperationDateRange
    {

        private System.DateTime startDateField;

        private System.DateTime endDateField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "date")]
        public System.DateTime StartDate
        {
            get
            {
                return this.startDateField;
            }
            set
            {
                this.startDateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "date")]
        public System.DateTime EndDate
        {
            get
            {
                return this.endDateField;
            }
            set
            {
                this.endDateField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeServicesServiceOperatingProfileBankHolidayOperation
    {

        private TransXChangeServicesServiceOperatingProfileBankHolidayOperationDaysOfOperation daysOfOperationField;

        private object daysOfNonOperationField;

        /// <remarks/>
        public TransXChangeServicesServiceOperatingProfileBankHolidayOperationDaysOfOperation DaysOfOperation
        {
            get
            {
                return this.daysOfOperationField;
            }
            set
            {
                this.daysOfOperationField = value;
            }
        }

        /// <remarks/>
        public object DaysOfNonOperation
        {
            get
            {
                return this.daysOfNonOperationField;
            }
            set
            {
                this.daysOfNonOperationField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeServicesServiceOperatingProfileBankHolidayOperationDaysOfOperation
    {

        private object goodFridayField;

        private object mayDayField;

        private object easterMondayField;

        private object springBankField;

        /// <remarks/>
        public object GoodFriday
        {
            get
            {
                return this.goodFridayField;
            }
            set
            {
                this.goodFridayField = value;
            }
        }

        /// <remarks/>
        public object MayDay
        {
            get
            {
                return this.mayDayField;
            }
            set
            {
                this.mayDayField = value;
            }
        }

        /// <remarks/>
        public object EasterMonday
        {
            get
            {
                return this.easterMondayField;
            }
            set
            {
                this.easterMondayField = value;
            }
        }

        /// <remarks/>
        public object SpringBank
        {
            get
            {
                return this.springBankField;
            }
            set
            {
                this.springBankField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeServicesServiceStopRequirements
    {

        private object noNewStopsRequiredField;

        /// <remarks/>
        public object NoNewStopsRequired
        {
            get
            {
                return this.noNewStopsRequiredField;
            }
            set
            {
                this.noNewStopsRequiredField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeServicesServiceStandardService
    {

        private string originField;

        private string destinationField;

        private TransXChangeServicesServiceStandardServiceJourneyPattern[] journeyPatternField;

        /// <remarks/>
        public string Origin
        {
            get
            {
                return this.originField;
            }
            set
            {
                this.originField = value;
            }
        }

        /// <remarks/>
        public string Destination
        {
            get
            {
                return this.destinationField;
            }
            set
            {
                this.destinationField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("JourneyPattern")]
        public TransXChangeServicesServiceStandardServiceJourneyPattern[] JourneyPattern
        {
            get
            {
                return this.journeyPatternField;
            }
            set
            {
                this.journeyPatternField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeServicesServiceStandardServiceJourneyPattern
    {

        private string directionField;

        private TransXChangeServicesServiceStandardServiceJourneyPatternOperational operationalField;

        private string routeRefField;

        private string journeyPatternSectionRefsField;

        private string idField;

        /// <remarks/>
        public string Direction
        {
            get
            {
                return this.directionField;
            }
            set
            {
                this.directionField = value;
            }
        }

        /// <remarks/>
        public TransXChangeServicesServiceStandardServiceJourneyPatternOperational Operational
        {
            get
            {
                return this.operationalField;
            }
            set
            {
                this.operationalField = value;
            }
        }

        /// <remarks/>
        public string RouteRef
        {
            get
            {
                return this.routeRefField;
            }
            set
            {
                this.routeRefField = value;
            }
        }

        /// <remarks/>
        public string JourneyPatternSectionRefs
        {
            get
            {
                return this.journeyPatternSectionRefsField;
            }
            set
            {
                this.journeyPatternSectionRefsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeServicesServiceStandardServiceJourneyPatternOperational
    {

        private TransXChangeServicesServiceStandardServiceJourneyPatternOperationalVehicleType vehicleTypeField;

        /// <remarks/>
        public TransXChangeServicesServiceStandardServiceJourneyPatternOperationalVehicleType VehicleType
        {
            get
            {
                return this.vehicleTypeField;
            }
            set
            {
                this.vehicleTypeField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeServicesServiceStandardServiceJourneyPatternOperationalVehicleType
    {

        private string vehicleTypeCodeField;

        private string descriptionField;

        /// <remarks/>
        public string VehicleTypeCode
        {
            get
            {
                return this.vehicleTypeCodeField;
            }
            set
            {
                this.vehicleTypeCodeField = value;
            }
        }

        /// <remarks/>
        public string Description
        {
            get
            {
                return this.descriptionField;
            }
            set
            {
                this.descriptionField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeVehicleJourney
    {

        private string privateCodeField;

        private TransXChangeVehicleJourneyOperational operationalField;

        private TransXChangeVehicleJourneyOperatingProfile operatingProfileField;

        private string vehicleJourneyCodeField;

        private string serviceRefField;

        private string lineRefField;

        private string journeyPatternRefField;

        private System.DateTime departureTimeField;

        /// <remarks/>
        public string PrivateCode
        {
            get
            {
                return this.privateCodeField;
            }
            set
            {
                this.privateCodeField = value;
            }
        }

        /// <remarks/>
        public TransXChangeVehicleJourneyOperational Operational
        {
            get
            {
                return this.operationalField;
            }
            set
            {
                this.operationalField = value;
            }
        }

        /// <remarks/>
        public TransXChangeVehicleJourneyOperatingProfile OperatingProfile
        {
            get
            {
                return this.operatingProfileField;
            }
            set
            {
                this.operatingProfileField = value;
            }
        }

        /// <remarks/>
        public string VehicleJourneyCode
        {
            get
            {
                return this.vehicleJourneyCodeField;
            }
            set
            {
                this.vehicleJourneyCodeField = value;
            }
        }

        /// <remarks/>
        public string ServiceRef
        {
            get
            {
                return this.serviceRefField;
            }
            set
            {
                this.serviceRefField = value;
            }
        }

        /// <remarks/>
        public string LineRef
        {
            get
            {
                return this.lineRefField;
            }
            set
            {
                this.lineRefField = value;
            }
        }

        /// <remarks/>
        public string JourneyPatternRef
        {
            get
            {
                return this.journeyPatternRefField;
            }
            set
            {
                this.journeyPatternRefField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "time")]
        public System.DateTime DepartureTime
        {
            get
            {
                return this.departureTimeField;
            }
            set
            {
                this.departureTimeField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeVehicleJourneyOperational
    {

        private TransXChangeVehicleJourneyOperationalVehicleType vehicleTypeField;

        /// <remarks/>
        public TransXChangeVehicleJourneyOperationalVehicleType VehicleType
        {
            get
            {
                return this.vehicleTypeField;
            }
            set
            {
                this.vehicleTypeField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeVehicleJourneyOperationalVehicleType
    {

        private string vehicleTypeCodeField;

        private string descriptionField;

        /// <remarks/>
        public string VehicleTypeCode
        {
            get
            {
                return this.vehicleTypeCodeField;
            }
            set
            {
                this.vehicleTypeCodeField = value;
            }
        }

        /// <remarks/>
        public string Description
        {
            get
            {
                return this.descriptionField;
            }
            set
            {
                this.descriptionField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeVehicleJourneyOperatingProfile
    {

        private TransXChangeVehicleJourneyOperatingProfileRegularDayType regularDayTypeField;

        private TransXChangeVehicleJourneyOperatingProfileSpecialDaysOperation specialDaysOperationField;

        private TransXChangeVehicleJourneyOperatingProfileBankHolidayOperation bankHolidayOperationField;

        /// <remarks/>
        public TransXChangeVehicleJourneyOperatingProfileRegularDayType RegularDayType
        {
            get
            {
                return this.regularDayTypeField;
            }
            set
            {
                this.regularDayTypeField = value;
            }
        }

        /// <remarks/>
        public TransXChangeVehicleJourneyOperatingProfileSpecialDaysOperation SpecialDaysOperation
        {
            get
            {
                return this.specialDaysOperationField;
            }
            set
            {
                this.specialDaysOperationField = value;
            }
        }

        /// <remarks/>
        public TransXChangeVehicleJourneyOperatingProfileBankHolidayOperation BankHolidayOperation
        {
            get
            {
                return this.bankHolidayOperationField;
            }
            set
            {
                this.bankHolidayOperationField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeVehicleJourneyOperatingProfileRegularDayType
    {

        private TransXChangeVehicleJourneyOperatingProfileRegularDayTypeDaysOfWeek daysOfWeekField;

        /// <remarks/>
        public TransXChangeVehicleJourneyOperatingProfileRegularDayTypeDaysOfWeek DaysOfWeek
        {
            get
            {
                return this.daysOfWeekField;
            }
            set
            {
                this.daysOfWeekField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeVehicleJourneyOperatingProfileRegularDayTypeDaysOfWeek
    {

        private object mondayField;

        private object tuesdayField;

        private object wednesdayField;

        private object thursdayField;

        private object sundayField;

        private object saturdayField;

        private object fridayField;

        /// <remarks/>
        public object Monday
        {
            get
            {
                return this.mondayField;
            }
            set
            {
                this.mondayField = value;
            }
        }

        /// <remarks/>
        public object Tuesday
        {
            get
            {
                return this.tuesdayField;
            }
            set
            {
                this.tuesdayField = value;
            }
        }

        /// <remarks/>
        public object Wednesday
        {
            get
            {
                return this.wednesdayField;
            }
            set
            {
                this.wednesdayField = value;
            }
        }

        /// <remarks/>
        public object Thursday
        {
            get
            {
                return this.thursdayField;
            }
            set
            {
                this.thursdayField = value;
            }
        }

        /// <remarks/>
        public object Sunday
        {
            get
            {
                return this.sundayField;
            }
            set
            {
                this.sundayField = value;
            }
        }

        /// <remarks/>
        public object Saturday
        {
            get
            {
                return this.saturdayField;
            }
            set
            {
                this.saturdayField = value;
            }
        }

        /// <remarks/>
        public object Friday
        {
            get
            {
                return this.fridayField;
            }
            set
            {
                this.fridayField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeVehicleJourneyOperatingProfileSpecialDaysOperation
    {

        private TransXChangeVehicleJourneyOperatingProfileSpecialDaysOperationDateRange[] daysOfNonOperationField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("DateRange", IsNullable = false)]
        public TransXChangeVehicleJourneyOperatingProfileSpecialDaysOperationDateRange[] DaysOfNonOperation
        {
            get
            {
                return this.daysOfNonOperationField;
            }
            set
            {
                this.daysOfNonOperationField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeVehicleJourneyOperatingProfileSpecialDaysOperationDateRange
    {

        private System.DateTime startDateField;

        private System.DateTime endDateField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "date")]
        public System.DateTime StartDate
        {
            get
            {
                return this.startDateField;
            }
            set
            {
                this.startDateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "date")]
        public System.DateTime EndDate
        {
            get
            {
                return this.endDateField;
            }
            set
            {
                this.endDateField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeVehicleJourneyOperatingProfileBankHolidayOperation
    {

        private TransXChangeVehicleJourneyOperatingProfileBankHolidayOperationDaysOfOperation daysOfOperationField;

        private TransXChangeVehicleJourneyOperatingProfileBankHolidayOperationDaysOfNonOperation daysOfNonOperationField;

        /// <remarks/>
        public TransXChangeVehicleJourneyOperatingProfileBankHolidayOperationDaysOfOperation DaysOfOperation
        {
            get
            {
                return this.daysOfOperationField;
            }
            set
            {
                this.daysOfOperationField = value;
            }
        }

        /// <remarks/>
        public TransXChangeVehicleJourneyOperatingProfileBankHolidayOperationDaysOfNonOperation DaysOfNonOperation
        {
            get
            {
                return this.daysOfNonOperationField;
            }
            set
            {
                this.daysOfNonOperationField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeVehicleJourneyOperatingProfileBankHolidayOperationDaysOfOperation
    {

        private object goodFridayField;

        private object mayDayField;

        private object easterMondayField;

        private object springBankField;

        /// <remarks/>
        public object GoodFriday
        {
            get
            {
                return this.goodFridayField;
            }
            set
            {
                this.goodFridayField = value;
            }
        }

        /// <remarks/>
        public object MayDay
        {
            get
            {
                return this.mayDayField;
            }
            set
            {
                this.mayDayField = value;
            }
        }

        /// <remarks/>
        public object EasterMonday
        {
            get
            {
                return this.easterMondayField;
            }
            set
            {
                this.easterMondayField = value;
            }
        }

        /// <remarks/>
        public object SpringBank
        {
            get
            {
                return this.springBankField;
            }
            set
            {
                this.springBankField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.transxchange.org.uk/")]
    public partial class TransXChangeVehicleJourneyOperatingProfileBankHolidayOperationDaysOfNonOperation
    {

        private object goodFridayField;

        private object mayDayField;

        private object easterMondayField;

        private object springBankField;

        private object allBankHolidaysField;

        /// <remarks/>
        public object GoodFriday
        {
            get
            {
                return this.goodFridayField;
            }
            set
            {
                this.goodFridayField = value;
            }
        }

        /// <remarks/>
        public object MayDay
        {
            get
            {
                return this.mayDayField;
            }
            set
            {
                this.mayDayField = value;
            }
        }

        /// <remarks/>
        public object EasterMonday
        {
            get
            {
                return this.easterMondayField;
            }
            set
            {
                this.easterMondayField = value;
            }
        }

        /// <remarks/>
        public object SpringBank
        {
            get
            {
                return this.springBankField;
            }
            set
            {
                this.springBankField = value;
            }
        }

        /// <remarks/>
        public object AllBankHolidays
        {
            get
            {
                return this.allBankHolidaysField;
            }
            set
            {
                this.allBankHolidaysField = value;
            }
        }
    }

}