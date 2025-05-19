using System.Text;
using System.Security.Cryptography;
using Konscious.Security.Cryptography;
using DotNetEnv;

namespace DbScriptGen
{
    class Program
    {
        private static string _pepper;
        private static DateTime now = DateTime.UtcNow;
        
        static void Main(string[] args)
        {
            Console.WriteLine("Starting script generation...");
            
            try
            {
                // Load environment variables from .env file in the parent directory
                Env.Load("../.env");
                _pepper = Env.GetString("PASSWORD_PEPPER");
                
                if (string.IsNullOrEmpty(_pepper))
                {
                    Console.WriteLine("ERROR: PASSWORD_PEPPER not found in .env file");
                    return;
                }
                
                var sqlScript = new StringBuilder();
                
                // Create users
                var users = CreateUsers();
                AppendUsersSql(sqlScript, users);
                
                // Create flashcard sets
                var sets = CreateFlashcardSets(users);
                AppendFlashcardSetsSql(sqlScript, sets);
                
                // Create flashcards
                var flashcards = CreateFlashcards(sets);
                AppendFlashcardsSql(sqlScript, flashcards);
                
                // Save to file
                File.WriteAllText("db_seed_script.sql", sqlScript.ToString());
                Console.WriteLine("Script generated successfully! Saved to db_seed_script.sql");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
        
        static List<User> CreateUsers()
        {
            var users = new List<User>();
            
            var turkishNames = new List<string> {
                "Yavuz", "Bugra", "Enes", "Batu", "Ece", "Ayşe", "Halil", "Zeynep", "Kerem", "Şeyma"
            };
            
            int id = 1;
            foreach (var name in turkishNames)
            {
                var (hash, salt) = HashPassword("12345678");
                
                var user = new User
                {
                    Id = id++,
                    Username = name.ToLower(),
                    Email = $"{name.ToLower()}@example.com",
                    PasswordHash = hash,
                    Salt = salt,
                    Bio = $"{name}'s profile",
                    FirstName = name,
                    LastName = "User",
                    IsAdmin = name == "Sefa",  // Make Sefa an admin
                    CreatedAt = now,
                    UpdatedAt = now
                };
                
                users.Add(user);
            }
            
            return users;
        }
        
        static List<FlashcardSet> CreateFlashcardSets(List<User> users)
        {
            var sets = new List<FlashcardSet>();
            var id = 1;
            var setThemes = new List<(string Title, string Description)>
            {
                ("Computer Engineering Fundamentals", "Basic concepts in computer engineering"),
                ("Data Structures and Algorithms", "Essential algorithms and data structures"),
                ("Frontend Development", "HTML, CSS, JavaScript and frameworks"),
                ("Backend Development", "Server-side programming and databases"),
                ("Mobile App Development", "iOS and Android development"),
                ("Database Systems", "SQL, NoSQL and database concepts"),
                ("Operating Systems", "OS concepts and architecture"),
                ("Network Fundamentals", "Networking and protocols"),
                ("Medicine Basics", "Fundamental medical concepts"),
                ("Pharmacology", "Drug classifications and effects"),
                ("Pokemon Collection", "Famous pokemon and their types")
            };
            
            var random = new Random();
            
            foreach (var user in users)
            {
                // Number of sets for this user (1-3)
                int numSets = random.Next(1, 4);
                
                // Get random set themes without repeating
                var shuffledThemes = setThemes.OrderBy(x => random.Next()).ToList();
                
                for (int i = 0; i < numSets && i < shuffledThemes.Count; i++)
                {
                    var theme = shuffledThemes[i];
                    
                    // Make sure the Pokemon set is assigned to user with ID 4 (Batu)
                    if (theme.Title.Contains("Pokemon") && user.Id != 4)
                    {
                        // Skip and get the next theme
                        if (i + 1 < shuffledThemes.Count)
                        {
                            theme = shuffledThemes[i + 1];
                        }
                        else if (i > 0)
                        {
                            theme = shuffledThemes[i - 1];
                        }
                    }
                    
                    // Create a set for this user
                    var set = new FlashcardSet
                    {
                        Id = id++,
                        UserId = user.Id,
                        Title = theme.Title,
                        Description = theme.Description,
                        Visibility = (Visibility)random.Next(3),  // Random visibility level
                        CreatedAt = now,
                        UpdatedAt = now,
                    };
                    
                    sets.Add(set);
                }
                
                // Make sure Pokemon set is assigned to Batu (ID 4)
                if (user.Id == 4 && !sets.Any(s => s.UserId == 4 && s.Title.Contains("Pokemon")))
                {
                    var pokemonSet = new FlashcardSet
                    {
                        Id = id++,
                        UserId = user.Id,
                        Title = "Pokemon Collection",
                        Description = "Famous pokemon and their types",
                        Visibility = Visibility.Public,
                        CreatedAt = now,
                        UpdatedAt = now,
                    };
                    sets.Add(pokemonSet);
                }
            }
            
            return sets;
        }
        
        static List<Flashcard> CreateFlashcards(List<FlashcardSet> sets)
        {
            var flashcards = new List<Flashcard>();
            var id = 1;
            var random = new Random();
            
            // Define flashcard content for different set themes
            var flashcardContent = new Dictionary<string, List<(string Term, string Definition)>>
            {
                ["Computer Engineering Fundamentals"] = new List<(string, string)>
                {
                    ("CPU", "Central Processing Unit - the 'brain' of the computer that executes instructions"),
                    ("RAM", "Random Access Memory - temporary storage used by running programs"),
                    ("Algorithm", "A step-by-step procedure for solving a problem or accomplishing a task"),
                    ("Bit", "The smallest unit of digital information (0 or 1)"),
                    ("Boolean Logic", "A form of algebra dealing with true/false values and operations"),
                    ("Cache", "A high-speed storage mechanism for temporary storage of data"),
                    ("Compiler", "A program that translates source code into machine code"),
                    ("Interpreter", "A program that executes instructions written in a programming language"),
                    ("Operating System", "Software that manages computer hardware and software resources"),
                    ("Network", "A group of interconnected computers that can communicate with each other"),
                    ("Protocol", "A set of rules governing the exchange of data between devices"),
                    ("Database", "An organized collection of structured information or data"),
                    ("Cloud Computing", "Delivery of computing services over the internet"),
                    ("Virtualization", "Creating a virtual version of something, such as a server or storage device"),
                    ("Machine Learning", "A subset of AI that enables systems to learn from data"),
                    ("Artificial Intelligence", "Simulation of human intelligence in machines"),
                    ("Data Mining", "The process of discovering patterns in large datasets"),
                    ("Blockchain", "A decentralized digital ledger used to record transactions across many computers"),
                    ("Cybersecurity", "The practice of protecting systems, networks, and programs from digital attacks"),
                    ("Encryption", "The process of converting information into a code to prevent unauthorized access"),
                    ("Decryption", "The process of converting encrypted data back into its original form"),
                    ("Network Topology", "The arrangement of different elements (links, nodes, etc.) in a computer network"),
                    ("IP Address", "A unique address that identifies a device on the internet or a local network"),
                    ("Subnetting", "Dividing a network into smaller, manageable sub-networks"),
                    ("Firewall", "A network security system that monitors and controls incoming and outgoing network traffic"),
                    ("Load Balancing", "Distributing workloads across multiple computing resources"),
                    ("Data Structure", "A particular way of organizing and storing data in a computer"),
                    ("Software Development Life Cycle", "A process for planning, creating, testing, and deploying software"),
                    ("Agile Methodology", "An iterative approach to software development that emphasizes flexibility and customer satisfaction"),
                    ("Version Control", "A system that records changes to a file or set of files over time"),
                    ("Git", "A distributed version control system for tracking changes in source code"),
                    ("Continuous Integration", "Development practice where developers integrate code into a shared repository several times a day"),
                    ("Continuous Deployment", "Software release process where every change that passes automated tests is deployed to production"),
                    ("API", "Application Programming Interface - a set of rules that allows programs to talk to each other"),
                    ("Microservices", "Architectural style that structures an application as a collection of loosely coupled services"),
                    ("DevOps", "A set of practices that combines software development and IT operations"),
                    ("Containerization", "A lightweight alternative to full machine virtualization that involves encapsulating an application in a container"),
                    ("Serverless Computing", "Cloud computing execution model where the cloud provider dynamically manages the allocation of machine resources"),
                    ("Big Data", "Extremely large data sets that may be analyzed computationally to reveal patterns, trends, and associations"),
                    ("Data Science", "Field that uses scientific methods, processes, algorithms, and systems to extract knowledge from data"),
                    ("Data Visualization", "The graphical representation of information and data"),
                    ("User Interface", "The means by which a user interacts with a computer or software"),
                    ("User Experience", "A person's emotions and attitudes about using a particular product, system, or service"),
                    ("Responsive Design", "An approach to web design that makes web pages render well on a variety of devices and window or screen sizes"),
                    ("Cross-Platform Development", "Creating software that can run on multiple operating systems or platforms"),
                    ("Integrated Development Environment", "A software application that provides comprehensive facilities to programmers for software development"),
                    ("Debugging", "The process of finding and resolving bugs or defects in a computer program"),
                    ("Software Testing", "The process of evaluating and verifying that a software program or application meets the requirements"),
                    ("User Acceptance Testing", "A type of testing performed by end-users to validate the usability and functionality of a system"),
                    ("Unit Testing", "A software testing method by which individual units of source code are tested"),
                    ("Integration Testing", "Testing the interfaces between components or systems"),
                    ("System Testing", "Testing the complete and integrated software product"),
                    ("Regression Testing", "Testing existing software applications to make sure that a change or addition hasn't broken any existing functionality"),
                    ("Performance Testing", "Testing to determine how a system performs in terms of responsiveness and stability under a particular workload"),
                    ("Load Testing", "Testing the system under heavy load to see how it behaves"),
                    ("Stress Testing", "Testing to evaluate how a system behaves under extreme conditions"),
                    ("Usability Testing", "Evaluating a product or service by testing it with real users"),
                    ("Security Testing", "Testing to uncover vulnerabilities, threats, and risks in a software application"),
                    ("Penetration Testing", "Simulated cyber attack against your computer system to check for exploitable vulnerabilities"),
                    ("Ethical Hacking", "Legally breaking into computers and devices to test an organization's defenses"),
                    ("Phishing", "Fraudulent attempt to obtain sensitive information by disguising as a trustworthy entity"),
                    ("Malware", "Software designed to disrupt, damage, or gain unauthorized access to computer systems"),
                    ("Ransomware", "Type of malicious software designed to block access to a computer system until a sum of money is paid"),
                    ("Spyware", "Software that enables a user to obtain covert information about another's computer activities"),
                    ("Adware", "Software that automatically displays or downloads advertising material when a user is online"),
                    ("Trojan Horse", "Malicious code disguised as legitimate software"),
                    ("Virus", "A type of malicious software that replicates itself by inserting copies into other computer programs"),
                    ("Worm", "A standalone malware computer program that replicates itself in order to spread to other computers"),
                    ("Trojan", "A type of malware that disguises itself as legitimate software"),
                    ("Rootkit", "A collection of computer software, typically malicious, designed to enable access to a computer or area of its software"),
                    ("Spyware", "Software that enables a user to obtain covert information about another's computer activities"),
                    ("Adware", "Software that automatically displays or downloads advertising material when a user is online"),
                    ("Keylogger", "A type of surveillance software that records keystrokes made by a user"),
                    ("Phishing", "Fraudulent attempt to obtain sensitive information by disguising as a trustworthy entity"),
                    ("Social Engineering", "Psychological manipulation of people into performing actions or divulging confidential information")
                },
                ["Data Structures and Algorithms"] = new List<(string, string)>
                {
                    ("Array", "A collection of elements stored at contiguous memory locations"),
                    ("Linked List", "A linear data structure where elements are stored in nodes with references to the next node"),
                    ("Stack", "LIFO (Last In First Out) data structure with push and pop operations"),
                    ("Queue", "FIFO (First In First Out) data structure with enqueue and dequeue operations"),
                    ("Binary Search", "A search algorithm that finds the position of a target value within a sorted array"),
                    ("Merge Sort", "An efficient, stable, comparison-based, divide and conquer sorting algorithm"),
                    ("Quick Sort", "An efficient sorting algorithm that uses divide and conquer"),
                    ("Hash Table", "A data structure that implements an associative array abstract data type"),
                    ("Graph", "A collection of nodes connected by edges"),
                    ("Tree", "A hierarchical data structure with a root node and child nodes"),
                    ("Binary Tree", "A tree data structure where each node has at most two children"),
                    ("Dynamic Programming", "A method for solving complex problems by breaking them down into simpler subproblems"),
                    ("Recursion", "The process of defining a function in terms of itself"),
                    ("Big O Notation", "Mathematical notation to describe the upper limit of an algorithm's running time"),
                    ("Time Complexity", "Computational complexity that describes the amount of time it takes to run an algorithm"),
                    ("Space Complexity", "Computational complexity that describes the amount of memory space required by an algorithm"),
                    ("Graph Traversal", "The process of visiting all the nodes in a graph"),
                    ("Depth-First Search", "An algorithm for traversing or searching tree or graph data structures"),
                    ("Breadth-First Search", "An algorithm for traversing or searching tree or graph data structures"),
                    ("Dijkstra's Algorithm", "An algorithm for finding the shortest paths between nodes in a graph"),
                    ("A* Search Algorithm", "An informed search algorithm that uses heuristics to find the shortest path"),
                    ("Binary Search Tree", "A tree data structure in which each node has at most two children, with left child < parent < right child"),
                    ("AVL Tree", "A self-balancing binary search tree where the difference in heights between left and right subtrees is at most one"),
                    ("Red-Black Tree", "A balanced binary search tree with properties that ensure the tree remains approximately balanced during insertions and deletions"),
                    ("Heap", "A special tree-based data structure that satisfies the heap property"),
                    ("Priority Queue", "An abstract data type where each element has a priority and elements are served based on priority"),
                    ("Trie", "A tree-like data structure that stores a dynamic set of strings, typically used for searching")
                },
                ["Frontend Development"] = new List<(string, string)>
                {
                    ("HTML", "HyperText Markup Language - the standard language for creating web pages"),
                    ("CSS", "Cascading Style Sheets - used for styling and layout of web pages"),
                    ("JavaScript", "A programming language that enables interactive web pages"),
                    ("React", "A JavaScript library for building user interfaces"),
                    ("DOM", "Document Object Model - programming interface for web documents"),
                    ("Responsive Design", "Design approach to make web pages render well on different devices/screen sizes"),
                    ("Bootstrap", "A front-end framework for developing responsive and mobile-first websites"),
                    ("Sass", "Syntactically Awesome Style Sheets - a preprocessor scripting language that is interpreted or compiled into CSS"),
                    ("Webpack", "A module bundler for JavaScript applications"),
                    ("AJAX", "Asynchronous JavaScript and XML - a set of web development techniques for creating asynchronous web applications"),
                    ("JSON", "JavaScript Object Notation - a lightweight data interchange format"),
                    ("REST API", "Representational State Transfer Application Programming Interface - an architectural style for designing networked applications"),
                    ("Single Page Application", "Web application that interacts with the user by dynamically rewriting the current page"),
                    ("Progressive Web App", "Web application that uses modern web capabilities to deliver an app-like experience"),
                    ("Cross-Browser Compatibility", "The ability of a website or web application to function across different browsers"),
                    ("Version Control", "System that records changes to a file or set of files over time so that you can recall specific versions later"),
                    ("Git", "Distributed version control system for tracking changes in source code during software development"),
                    ("CSS Grid", "A CSS layout system for creating complex web layouts"),
                    ("Flexbox", "A one-dimensional layout method for arranging items in a container"),
                    ("Viewport", "The user's visible area of a web page"),
                    ("Media Query", "A CSS technique used to apply styles based on the viewport size"),
                    ("Accessibility", "Designing websites that are usable by people with disabilities"),
                    ("SEO", "Search Engine Optimization - the process of improving the visibility of a website in search engines"),
                    ("Web Performance", "The speed at which web pages are downloaded and displayed"),
                    ("Content Delivery Network", "A system of distributed servers that deliver web content to users based on their geographic location"),
                    ("Progressive Enhancement", "A strategy for web design that emphasizes core webpage content first"),
                    ("Responsive Images", "Images that scale nicely to fit any screen size"),
                    ("Viewport Meta Tag", "HTML tag used to control the layout on mobile browsers")
                },
                ["Backend Development"] = new List<(string, string)>
                {
                    ("API", "Application Programming Interface - a set of rules that allows programs to talk to each other"),
                    ("REST", "Representational State Transfer - an architectural style for designing networked applications"),
                    ("ORM", "Object-Relational Mapping - technique for converting data between incompatible type systems"),
                    ("Middleware", "Software that acts as a bridge between an operating system and applications"),
                    ("Authentication", "Process of verifying the identity of a user or process"),
                    ("Authorization", "Process of giving permission to do or have something"),
                    ("CRUD", "Create, Read, Update, Delete - basic operations for managing data"),
                    ("NoSQL", "Non-relational database management system that uses a variety of data models"),
                    ("GraphQL", "A query language for APIs and a runtime for executing those queries with your existing data"),
                    ("Microservices", "Architectural style that structures an application as a collection of loosely coupled services"),
                    ("Docker", "Platform for developing, shipping, and running applications in containers"),
                    ("Kubernetes", "Open-source system for automating deployment, scaling, and management of containerized applications"),
                    ("Load Balancer", "Distributes network or application traffic across multiple servers"),
                    ("WebSocket", "Communication protocol providing full-duplex communication channels over a single TCP connection"),
                    ("Session Management", "The process of securely handling user sessions in web applications"),
                    ("Caching", "Storing copies of files or data in a cache to reduce access time"),
                    ("Message Queue", "A form of asynchronous service-to-service communication used in serverless and microservices architectures"),
                    ("Serverless", "Cloud computing execution model where the cloud provider dynamically manages the allocation of machine resources"),
                    ("Load Testing", "Testing the system under heavy load to see how it behaves"),
                    ("Continuous Integration", "Development practice where developers integrate code into a shared repository several times a day"),
                    ("Continuous Deployment", "Software release process where every change that passes automated tests is deployed to production"),
                    ("Version Control", "System that records changes to a file or set of files over time so that you can recall specific versions later"),
                    ("Git", "Distributed version control system for tracking changes in source code during software development"),
                    ("CI/CD", "Continuous Integration/Continuous Deployment - practices that enable frequent code changes and automated deployment")
                },
                ["Mobile App Development"] = new List<(string, string)>
                {
                    ("Swift", "Programming language created by Apple for iOS development"),
                    ("Kotlin", "Modern programming language for Android development"),
                    ("React Native", "Framework for building native apps using React"),
                    ("Flutter", "Google's UI toolkit for building natively compiled applications"),
                    ("API Level", "An integer value that identifies the API revision offered by a version of Android"),
                    ("App Store", "Digital distribution platform for mobile apps on iOS"),
                    ("Play Store", "Digital distribution platform for mobile apps on Android"),
                    ("Xcode", "Apple's integrated development environment for macOS"),
                    ("Android Studio", "Official IDE for Android development"),
                    ("UI/UX Design", "User Interface/User Experience design - the process of enhancing user satisfaction"),
                    ("SDK", "Software Development Kit - a collection of software tools and libraries for building applications"),
                    ("Emulator", "Software that simulates hardware to run mobile apps on a computer"),
                    ("Push Notification", "Message sent from a server to a client device"),
                    ("App Bundle", "A publishing format that contains all of an app's compiled code and resources"),
                    ("Gradle", "Build automation tool used in Android development"),
                    ("CocoaPods", "Dependency manager for Swift and Objective-C Cocoa projects"),
                    ("Firebase", "Platform developed by Google for creating mobile and web applications"),
                    ("SwiftUI", "User interface toolkit that lets you design apps in a declarative way"),
                    ("Jetpack Compose", "Modern toolkit for building native Android UI"),
                    ("MVVM", "Model-View-ViewModel - software architectural pattern for designing user interfaces"),
                    ("MVC", "Model-View-Controller - software architectural pattern for implementing user interfaces"),
                    ("Material Design", "Design language developed by Google for Android and web applications")
                },
                ["Database Systems"] = new List<(string, string)>
                {
                    ("SQL", "Structured Query Language - used to communicate with a relational database"),
                    ("NoSQL", "Database that provides a mechanism for storage and retrieval of data not using tabular relations"),
                    ("ACID", "Atomicity, Consistency, Isolation, Durability - properties of database transactions"),
                    ("Index", "A data structure that improves the speed of data retrieval operations"),
                    ("Normalization", "Process of organizing data to reduce redundancy and improve integrity"),
                    ("JOIN", "SQL clause used to combine rows from two or more tables"),
                    ("Foreign Key", "A field in one table that uniquely identifies a row of another table"),
                    ("Primary Key", "A unique identifier for a record in a table"),
                    ("Transaction", "A sequence of operations performed as a single logical unit of work"),
                    ("Stored Procedure", "A set of SQL statements that can be stored and reused"),
                    ("Trigger", "A set of instructions that are automatically executed in response to certain events"),
                    ("View", "A virtual table based on the result set of a SQL statement"),
                    ("Replication", "The process of sharing information across multiple databases"),
                    ("Sharding", "A method of distributing data across multiple servers"),
                    ("Data Warehouse", "A system used for reporting and data analysis, and is considered a core component of business intelligence"),
                    ("ETL", "Extract, Transform, Load - process of moving data from one system to another"),
                    ("Data Lake", "A storage repository that holds a vast amount of raw data in its native format until it is needed"),
                    ("Columnar Database", "A database that stores data in columns rather than rows"),
                    ("Document Store", "A type of NoSQL database that stores data in documents instead of rows and columns"),
                    ("Graph Database", "A database designed to treat the relationships between data as equally important to the data itself"),
                    ("Key-Value Store", "A type of NoSQL database that uses a simple key-value method to store data"),
                    ("Time Series Database", "A database optimized for time-stamped or time series data"),
                    ("Object Storage", "A storage architecture that manages data as objects, as opposed to files or blocks")
                },
                ["Operating Systems"] = new List<(string, string)>
                {
                    ("Kernel", "Core component of an operating system with complete control over everything in the system"),
                    ("Process", "An instance of a computer program being executed"),
                    ("Thread", "A path of execution within a process"),
                    ("Deadlock", "A situation where two or more processes are unable to proceed because each is waiting for resources"),
                    ("Virtual Memory", "Memory management technique that provides an idealized abstraction of storage resources"),
                    ("File System", "Method and data structure that the OS uses to control how data is stored and retrieved"),
                    ("Semaphore", "Variable or abstract data type used to control access to a common resource in concurrent programming"),
                    ("Mutex", "Mutual exclusion - a synchronization primitive that allows multiple threads to share the same resource"),
                    ("Context Switch", "The process of storing and restoring the state of a CPU so that multiple processes can share a single CPU resource"),
                    ("Paging", "Memory management scheme that eliminates the need for contiguous allocation of physical memory"),
                    ("Thread Pool", "A collection of threads that can be reused to perform tasks"),
                    ("File Descriptor", "An abstract indicator for accessing a file or other input/output resource"),
                    ("Shell", "Command-line interface for interacting with the operating system"),
                    ("System Call", "A programmatic way in which a computer program requests a service from the kernel"),
                    ("Daemon", "Background process that handles requests for services such as hardware or software"),
                    ("Bootloader", "Software that loads the operating system into memory"),
                    ("Scheduler", "Component of the operating system that manages the execution of processes"),
                    ("Interrupt", "Signal to the processor emitted by hardware or software indicating an event that needs immediate attention"),
                    ("Thread Safety", "Property of a data structure or code that guarantees safe execution by multiple threads")
                },
                ["Network Fundamentals"] = new List<(string, string)>
                {
                    ("IP Address", "Numerical label assigned to each device connected to a computer network"),
                    ("TCP", "Transmission Control Protocol - provides reliable, ordered, and error-checked delivery of data"),
                    ("DNS", "Domain Name System - translates domain names to IP addresses"),
                    ("HTTP", "Hypertext Transfer Protocol - foundation of data communication on the web"),
                    ("Router", "Networking device that forwards data packets between computer networks"),
                    ("Firewall", "Network security system that monitors and controls incoming and outgoing network traffic"),
                    ("VPN", "Virtual Private Network - extends a private network across a public network"),
                    ("LAN", "Local Area Network - a network that connects computers in a limited area"),
                    ("WAN", "Wide Area Network - a telecommunications network that extends over a large geographical area"),
                    ("OSI Model", "Conceptual framework used to understand network interactions in seven layers"),
                    ("Ethernet", "Family of computer networking technologies for local area networks"),
                    ("Wi-Fi", "Wireless networking technology that allows devices to communicate over a wireless signal"),
                    ("Bandwidth", "Maximum rate of data transfer across a network path"),
                    ("Latency", "Time taken for data to travel from source to destination"),
                    ("Packet", "Formatted unit of data carried by a packet-switched network"),
                    ("Protocol", "Set of rules governing the exchange of data between devices"),
                    ("Subnetting", "Dividing a network into smaller, manageable sub-networks"),
                    ("NAT", "Network Address Translation - method of remapping one IP address space into another"),
                    ("DHCP", "Dynamic Host Configuration Protocol - network management protocol used to automate the process of configuring devices"),
                    ("SSL/TLS", "Protocols for establishing authenticated and encrypted links between networked computers"),
                    ("VoIP", "Voice over Internet Protocol - technology that allows voice communication over the internet"),
                    ("IPv4", "Internet Protocol version 4 - uses 32-bit addresses"),
                    ("IPv6", "Internet Protocol version 6 - uses 128-bit addresses"),
                    ("OSI Model Layers", "Application, Presentation, Session, Transport, Network, Data Link, Physical"),
                    ("OSI Model Layer 1", "Physical Layer - deals with the physical connection between devices"),
                    ("OSI Model Layer 2", "Data Link Layer - provides node-to-node data transfer"),
                    ("OSI Model Layer 3", "Network Layer - handles routing of data packets"),
                    ("OSI Model Layer 4", "Transport Layer - provides reliable or unreliable delivery"),
                    ("OSI Model Layer 5", "Session Layer - manages sessions between applications"),
                    ("OSI Model Layer 6", "Presentation Layer - translates data formats"),
                    ("OSI Model Layer 7", "Application Layer - closest to the end user")
                },
                ["Medicine Basics"] = new List<(string, string)>
                {
                    ("Anatomy", "Branch of biology concerned with the study of the structure of organisms"),
                    ("Physiology", "Branch of biology that deals with the normal functions of living organisms"),
                    ("Pathology", "Study of the causes and effects of disease or injury"),
                    ("Etiology", "Study of causation or origination of disease"),
                    ("Diagnosis", "Identification of the nature of an illness or problem"),
                    ("Prognosis", "Forecast of the likely outcome of a disease"),
                    ("Therapy", "Treatment intended to relieve or heal a disorder"),
                    ("Surgery", "Medical procedure involving an incision with instruments"),
                    ("Radiology", "Medical discipline that uses medical imaging to diagnose and treat diseases"),
                    ("Pharmacology", "Study of drugs and their effects on the body"),
                    ("Immunology", "Branch of medicine and biology concerned with immunity"),
                    ("Microbiology", "Study of microscopic organisms, such as bacteria, viruses, fungi, and protozoa"),
                    ("Biochemistry", "Study of chemical processes within and relating to living organisms"),
                    ("Genetics", "Study of genes, genetic variation, and heredity in living organisms"),
                    ("Epidemiology", "Study of how diseases affect the health and illness of populations"),
                    ("Toxicology", "Study of the adverse effects of chemicals on living organisms"),
                    ("Pharmacokinetics", "Study of how drugs move through the body"),
                    ("Pharmacodynamics", "Study of the effects of drugs on the body"),
                    ("Clinical Trials", "Research studies performed on people to evaluate a medical, surgical, or behavioral intervention"),
                    ("Anesthesia", "Use of drugs to prevent pain during surgery"),
                    ("Radiotherapy", "Treatment using radiation, typically as part of cancer treatment"),
                    ("Chemotherapy", "Use of drugs to treat cancer"),
                    ("Antibiotics", "Drugs that fight bacterial infections"),
                    ("Vaccination", "Administration of a vaccine to help the immune system develop protection from disease")
                },
                ["Pharmacology"] = new List<(string, string)>
                {
                    ("Drug Absorption", "Movement of a drug from its site of administration into the bloodstream"),
                    ("Half-life", "Time required for a quantity to reduce to half of its initial value"),
                    ("Therapeutic Index", "Ratio of the dose that produces toxicity to the dose needed for therapy"),
                    ("Receptor", "Protein molecule to which drugs bind to produce their effects"),
                    ("Agonist", "Drug that binds to a receptor and triggers a response"),
                    ("Antagonist", "Drug that blocks or dampens a biological response by binding to and blocking a receptor")
                },
                ["Pokemon Collection"] = new List<(string, string)>
                {
                    ("Gastly", "Ghost/Poison-type Pokemon."),
                    ("Pidgey", "Normal/Flying-type Pokemon known for its speed and agility"),
                    ("Eevee", "Normal-type Pokemon with multiple evolution options"),
                    ("Gyarados", "Water/Flying-type Pokemon known for its ferocity and power"),
                    ("Onix", "Rock/Ground-type Pokemon resembling a giant snake made of boulders"),
                    ("Machamp", "Fighting-type Pokemon known for its four arms and strength"),
                    ("Alakazam", "Psychic-type Pokemon known for its high intelligence and psychic abilities"),
                    ("Ditto", "Normal-type Pokemon that can transform into any other Pokemon"),
                    ("Lapras", "Water/Ice-type Pokemon known for its gentle nature and ability to ferry people across water"),
                    ("Gardevoir", "Psychic/Fairy-type Pokemon known for its loyalty and protective nature"),
                    ("Lucario", "Fighting/Steel-type Pokemon known for its aura-sensing abilities"),
                    ("Greninja", "Water/Dark-type Pokemon known for its speed and stealth"),
                    ("Togepi", "Fairy-type Pokemon known for its egg-like appearance and cheerful nature"),
                    ("Piplup", "Water-type starter Pokemon resembling a small penguin"),
                    ("Chikorita", "Grass-type starter Pokemon with a leaf on its head"),
                    ("Cyndaquil", "Fire-type starter Pokemon resembling a small, fire-breathing mammal"),
                    ("Treecko", "Grass-type starter Pokemon resembling a small lizard"),
                    ("Mudkip", "Water-type starter Pokemon resembling a small amphibian"),
                    ("Ponyta", "Fire-type Pokemon resembling a horse with a flaming mane"),
                    ("Snivy", "Grass-type starter Pokemon resembling a small snake"),
                    ("Turtwig", "Grass-type starter Pokemon resembling a small turtle"),
                    ("Zubat", "Poison/Flying-type Pokemon known for its echolocation abilities"),
                    ("Psyduck", "Water-type Pokemon known for its headache and psychic abilities"),
                    ("Pikachu", "Electric-type Pokemon known for its lightning bolt tail"),
                    ("Charizard", "Fire/Flying-type Pokemon that is the final evolution of Charmander"),
                    ("Bulbasaur", "Grass/Poison-type starter Pokemon with a plant bulb on its back"),
                    ("Squirtle", "Water-type starter Pokemon resembling a small turtle"),
                    ("Mewtwo", "Powerful Psychic-type Legendary Pokemon created by genetic manipulation"),
                    ("Jigglypuff", "Fairy/Normal-type Pokemon known for its singing ability that puts others to sleep"),
                    ("Snorlax", "Normal-type Pokemon known for its large size and sleepy nature"),
                    ("Gengar", "Ghost/Poison-type Pokemon that hides in shadows")
                }
            };
            
            foreach (var set in sets)
            {
                // Get content based on set title
                var content = flashcardContent
                    .FirstOrDefault(fc => set.Title.Contains(fc.Key)).Value;
                
                // If no specific content found, use a default set
                if (content == null || !content.Any())
                {
                    content = flashcardContent["Computer Engineering Fundamentals"];
                }
                
                // Create 5-8 flashcards per set
                int cardCount = random.Next(5, 9);
                cardCount = Math.Min(cardCount, content.Count);
                
                for (int i = 0; i < cardCount; i++)
                {
                    var flashcard = new Flashcard
                    {
                        Id = id++,
                        SetId = set.Id,
                        Term = content[i].Term,
                        Definition = content[i].Definition,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    
                    flashcards.Add(flashcard);
                }
            }
            
            return flashcards;
        }
        
        static void AppendUsersSql(StringBuilder sb, List<User> users)
        {
            sb.AppendLine("-- Users");
            sb.AppendLine("INSERT INTO `Users` (`Id`, `Username`, `Email`, `PasswordHash`, `Salt`, `Bio`, `FirstName`, `LastName`, `IsAdmin`, `CreatedAt`, `UpdatedAt`) VALUES");
            
            for (int i = 0; i < users.Count; i++)
            {
                var user = users[i];
                sb.Append($"({user.Id}, '{EscapeSql(user.Username)}', '{EscapeSql(user.Email)}', '{EscapeSql(user.PasswordHash)}', '{EscapeSql(user.Salt)}', ");
                sb.Append($"'{EscapeSql(user.Bio)}', '{EscapeSql(user.FirstName)}', '{EscapeSql(user.LastName)}', {(user.IsAdmin ? 1 : 0)}, ");
                sb.Append($"'{FormatDate(user.CreatedAt)}', '{FormatDate(user.UpdatedAt)}')");
                
                sb.AppendLine(i < users.Count - 1 ? "," : ";");
            }
            
            sb.AppendLine();
        }
        
        static void AppendFlashcardSetsSql(StringBuilder sb, List<FlashcardSet> sets)
        {
            sb.AppendLine("-- Flashcard Sets");
            sb.AppendLine("INSERT INTO `FlashcardSets` (`Id`, `UserId`, `Title`, `Description`, `Visibility`, `CoverImageUrl`, `CreatedAt`, `UpdatedAt`) VALUES");
            
            for (int i = 0; i < sets.Count; i++)
            {
                var set = sets[i];
                sb.Append($"({set.Id}, {set.UserId}, '{EscapeSql(set.Title)}', '{EscapeSql(set.Description)}', {(int)set.Visibility}, NULL, ");
                sb.Append($"'{FormatDate(set.CreatedAt)}', '{FormatDate(set.UpdatedAt)}')");
                
                sb.AppendLine(i < sets.Count - 1 ? "," : ";");
            }
            
            sb.AppendLine();
        }
        
        static void AppendFlashcardsSql(StringBuilder sb, List<Flashcard> flashcards)
        {
            sb.AppendLine("-- Flashcards");
            sb.AppendLine("INSERT INTO `Flashcards` (`Id`, `SetId`, `Term`, `Definition`, `ImageUrl`, `ExampleSentence`, `CreatedAt`, `UpdatedAt`) VALUES");
            
            for (int i = 0; i < flashcards.Count; i++)
            {
                var card = flashcards[i];
                sb.Append($"({card.Id}, {card.SetId}, '{EscapeSql(card.Term)}', '{EscapeSql(card.Definition)}', NULL, NULL, ");
                sb.Append($"'{FormatDate(card.CreatedAt)}', '{FormatDate(card.UpdatedAt)}')");
                
                sb.AppendLine(i < flashcards.Count - 1 ? "," : ";");
            }
            
            sb.AppendLine();
        }
        
        static string FormatDate(DateTime date)
        {
            return date.ToString("yyyy-MM-dd HH:mm:ss");
        }
        
        static string EscapeSql(string input)
        {
            if (input == null)
                return null;
            
            return input.Replace("'", "''");
        }
        
        static (string Hash, string Salt) HashPassword(string password)
        {
            // Generate a random salt
            byte[] saltBytes = new byte[32]; // 32 bytes = 256 bits
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }

            // Convert salt to base64 string for storage
            var salt = Convert.ToBase64String(saltBytes);
            
            // Get the hash using Argon2id
            byte[] hashBytes = HashPasswordWithArgon2id(password, saltBytes);
            
            // Convert hash to base64 string for storage
            var hash = Convert.ToBase64String(hashBytes);
            
            return (hash, salt);
        }
        
        static byte[] HashPasswordWithArgon2id(string password, byte[] salt)
        {
            // Combine password with pepper before hashing
            string pepperedPassword = password + _pepper;
            byte[] passwordBytes = Encoding.UTF8.GetBytes(pepperedPassword);

            // Create Argon2id instance
            using var argon2 = new Argon2id(passwordBytes)
            {
                Salt = salt,
                DegreeOfParallelism = 8, // Number of threads to use
                MemorySize = 65536,      // 64MB of memory
                Iterations = 4,          // Number of iterations
                KnownSecret = null,      // Additional secret if needed (we're using pepper separately)
                AssociatedData = null    // Additional data if needed
            };

            return argon2.GetBytes(32); // Get 32 bytes (256 bits) hash
        }
    }
    
    // Model classes
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Salt { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
    
    public enum Visibility
    {
        Public = 0,
        Friends = 1,
        Private = 2
    }
    
    public class FlashcardSet
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Visibility Visibility { get; set; } = Visibility.Public;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
    
    public class Flashcard
    {
        public int Id { get; set; }
        public int SetId { get; set; }
        public string Term { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
