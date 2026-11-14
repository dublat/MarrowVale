using MarrowVale.Business.Entities.Enums;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarrowVale.Business.Entities.Entities
{
    // This would most likely be a base class, and may want to make more enemy types that inherit this.
    public class Npc : GraphNode
    {
        public Npc()
        {
            IsAlive = true;
            Abilities = new List<Ability>();
            SpellBook = new List<Spell>();
            Items = new List<Item>();
            Relationships = new List<Relationship>();
            this.EntityLabel = "Character";
            this.Labels = new List<string>() { EntityLabel };
        }

        public Npc(IList<Dialogue> Dialogue, NpcRaceEnum Race, ClassEnum Class, string Name, string Description, int CurrentHealth, int MaxHealth, int BaseDamage, int age)
        {
            this.StartingDialogue = Dialogue;
            this.Race = Race;
            this.Class = Class;
            this.Name = Name;
            this.Description = Description;
            this.CurrentHealth = CurrentHealth;
            this.MaxHealth = MaxHealth;
            this.BaseDamage = BaseDamage;
            this.Age = age;
        }

        [JsonIgnore]
        public string FullName { get { return $"{this.Name} {this.LastName}";}}
        public string LastName { get; set; }
        public int Age { get; set; }
        public bool IsAlive { get; set; }
        public string Gender { get; set; }
        public decimal FeelingTowardsPlayer { get; set; }
        public List<string> ActivityHistory { get; set; }
        public string CurrentActivity { get; set; }
        public NpcTypeEnum Type { get; private set; }


        #region Purpose
        public string Objective { get; set; }
        public SentimentEnum Sentiment { get; set; }
        public OccupationEnum Occupation { get; set; }
        public string Religion { get; set; }
        public decimal Piousness { get; set; }
        #endregion

        #region Pysiology
        public int MaxHealth { get; set; }
        public int BaseDamage { get; set; }
        public int CurrentHealth { get; set; }
        public NpcRaceEnum Race { get; set; }
        public ClassEnum Class { get; set; }
        #endregion

        #region Personality
        public decimal Extraversion { get; set; }
        public decimal Conscientiousness { get; set; }
        public decimal Agreeableness { get; set; }
        public decimal Neuroticism { get; set; }
        public decimal Openness { get; set; }
        #endregion

        [JsonIgnore]
        public Armor Armor { get; set; }
        [JsonIgnore]
        public Weapon Weapon { get; set; }

        [JsonProperty]
        public bool Visible { get; private set; }
        [JsonIgnore]
        public IList<Relationship> Relationships { get; set; }
        public IList<Ability> Abilities { get; }
        public IList<Spell> SpellBook { get; }
        public IList<Item> Items { get; }
        [JsonIgnore]
        public Inventory Inventory { get; set; }

        private IList<Dialogue> StartingDialogue { get; }




        public void Show()
        {
            Visible = true;
        }

        public void Hide()
        {
            Visible = false;
        }

        public override string DescriptionPromptInput()
        {
            var input = new StringBuilder();
            //TODO: Give info based on vision/knowledge
            var isFamiliar = false;
            if (isFamiliar)
            {
                input.Append($"{FullName}");
                if (!string.IsNullOrEmpty(CurrentActivity))
                {
                    input.Append($" - They are {CurrentActivity.ToLower()}.\n");
                }
                return input.ToString();
            }

            input.Append($"You see a {Gender.ToLower()} {Race.ToString().ToLower()} named {FullName} who looks to be about {Age} years old.\n");
            input.Append($"They appear to be a {Occupation.ToString().ToLower()}.\n");
            input.Append($"They seem to be feeling {Sentiment.ToString().ToLower()} towards you.\n");
            if (!string.IsNullOrEmpty(CurrentActivity))
            {
                input.Append($" - They are {CurrentActivity.ToLower()}.\n");
            }

            return input.ToString();    
        }



        public string CombatDescription()
        {
            return $"Enemy: {Name} | Weapon: {Weapon?.Name} Armor: {Armor?.Name}";
        }



        public void AddRelationship(Npc relationTo, RelationEnum relation)
        {
            var relationShip = new Relationship
            {
                Toward = relationTo,
                Relation = relation
            };

            if (relation == RelationEnum.Married)
            {
                if (!this.Relationships.Any(x => x.Relation == RelationEnum.Married))
                {
                    this.Relationships.Add(relationShip);
                    relationTo.AddRelationship(this, RelationEnum.Married);
                }
            }
            else if (relation == RelationEnum.Parent)
            {
                this.Relationships.Add(relationShip);
                relationTo.AddRelationship(this, RelationEnum.Child);
            }
            else
            {
                this.Relationships.Add(relationShip);
            }


        }



    }
}
